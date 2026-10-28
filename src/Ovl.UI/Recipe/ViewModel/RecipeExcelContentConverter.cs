using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Model;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.TestInfo;
using Marvel.DataModels.Recipe40;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using WinCopies.Util;

namespace Ovl.UI.Recipe.ViewModel
{
    internal sealed class RecipeExcelContentConverter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessageBoxService _messageBoxService;

        public const string WaferInfo = "Wafer_info";
        public const string AlignmentInfo = "Alignment_info";
        public const string RunParameterInfo = "RunParameter_info";
        public const string TestInfo = "Test_info";

        public RecipeExcelContentConverter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _messageBoxService = serviceProvider.GetRequiredService<IMessageBoxService>();
        }

        /// <summary>
        /// 导出Recipe到Excel 通过ID，获取Recipe内容并转换成Excel内容对象列表，返回给调用方进行Excel导出
        /// </summary>
        /// <param name="recipeId"></param>
        /// <returns></returns>
        public List<RecipeContent> GetRecipeContentsById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return new List<RecipeContent>();
            }

            try
            {
                var recipeNodeService = _serviceProvider.GetRequiredService<IRecipeManager>();
                var recipe = recipeNodeService.GetCurrentRecipe(recipeId);
                if (recipe == null)
                {
                    return new List<RecipeContent>();
                }

                var recipeElements = recipe.Get<RecipeElementsInfo>();
                if (recipeElements == null)
                {
                    return new List<RecipeContent>();
                }

                return RecipeExcelContentBuilder.Build(recipeElements);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, $"GetRecipeContentsById failed, RecipeId={recipeId}");
                return new List<RecipeContent>();
            }
        }

        /// <summary>
        /// Excle导出到Recipe 通过Excel内容对象列表，转换成Recipe内容，并通过ID更新到对应的Recipe节点中
        /// </summary>
        /// <param name="contents"></param>
        public void ImportRecipeFromExcelContents(List<RecipeContent> contents)
        {
            if (contents == null || contents.Count == 0)
            {
                return;
            }

            try
            {
                var recipes = RecipeExcelContentParser.Parse(contents);
                if (recipes == null || recipes.Count == 0)
                {
                    return;
                }

                var recipeNodeService = _serviceProvider.GetRequiredService<IRecipeManager>();

                foreach (var recipeElements in recipes)
                {
                    if (recipeElements == null || string.IsNullOrWhiteSpace(recipeElements.Id))
                    {
                        continue;
                    }

                    var recipe = recipeNodeService.GetCurrentRecipe(recipeElements.Id);
                    if (recipe == null)
                    {
                        continue;
                    }
                    CreateWaferMapAfterImport(recipeElements);
                    recipe.Set(recipeElements, true);
                    LogHelper.LogInfo($"ImportRecipeFromExcel succeeded. RecipeId={recipeElements.Id}, RecipeName={recipeElements.Name}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "ImportRecipeFromExcelContents failed");
                _messageBoxService.ErrorDialog($"Import recipe content failed! {ex.Message}");
            }
        }
        
        private static class RecipeExcelContentBuilder
        {
            public static List<RecipeContent> Build(RecipeElementsInfo recipe)
            {
                var result = new List<RecipeContent>();
                if (recipe == null)
                {
                    return result;
                }

                result.Add(BuildContent(recipe.Id, RecipeExcelSheetNames.WaferInfo, recipe.Name, recipe.WaferMap));
                //result.Add(BuildContent(recipe.Id, RecipeExcelSheetNames.AlignmentInfo, recipe.Name, recipe.Alignment));
                //result.Add(BuildContent(recipe.Id, RecipeExcelSheetNames.RunParameterInfo, recipe.Name, recipe.RunParameter));
                result.AddRange(BuildTestContents(recipe.Id, recipe.Name, recipe.Tests));

                return result;
            }

            private static RecipeContent BuildContent(string recipeId, string recipeType, string recipeName, object model)
            {
                var fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Recipe_Name"] = recipeName
                };

                var mappedFields = ExcelMapReflectionHelper.ExportObject(model);
                foreach (var item in mappedFields)
                {
                    fields[item.Key] = item.Value;
                }

                return new RecipeContent
                {
                    Id = recipeId,
                    RecipeName = recipeName,
                    RecipeType = recipeType,
                    Fields = fields
                };
            }

            private static List<RecipeContent> BuildTestContents(string recipeId, string recipeName, IEnumerable<TestInfoBase> tests)
            {
                var result = new List<RecipeContent>();
                if (tests == null)
                {
                    return result;
                }

                foreach (var test in tests)
                {
                    if (test == null)
                    {
                        continue;
                    }

                    if (test.IsEnable==false)
                    {//test的状态为false的，不导出。
                        continue;
                    }
                    foreach (var row in BuildTestRows(recipeId, recipeName, test))
                    {
                        result.Add(row);
                    }
                }

                return result;
            }

            private static IEnumerable<RecipeContent> BuildTestRows(string recipeId, string recipeName, TestInfoBase test)
            {
                var targets = GetTestTargets(test);
                if (targets == null)
                {
                    yield break;
                }

                foreach (var item in targets)
                {
                    var fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Recipe_Name"] = recipeName,
                        ["Test_Name"] = GetTestName(test),
                        //["Target_ID"] = item.Target?.Id ?? string.Empty,
                        ["Test_Type"] = GetTestTypeName(test),
                        ["TargetSamefields"] = test.TargetSetting?.TargetSamefields ?? true,
                        ["Origin_Point"] = item.Target?.OrgionPoint.ToString() ?? string.Empty,
                        ["Field_X"] = item.FieldX,
                        ["Field_Y"] = item.FieldY,
                        ["Target_Position(R3)_X(mm)"] = item.Target?.R3Vector?.X ?? 0d,
                        ["Target_Position(R3)_Y(mm)"] = item.Target?.R3Vector?.Y ?? 0d
                    };

                    AddFaceModeField(fields, test);

                    yield return new RecipeContent
                    {
                        Id = recipeId,
                        RecipeName = recipeName,
                        RecipeType = RecipeExcelSheetNames.TestInfo,
                        Fields = fields
                    };
                }
            }

            private static void AddFaceModeField(IDictionary<string, object> fields, TestInfoBase test)
            {
                if (fields == null || test == null)
                {
                    return;
                }

                var faceModeProperty = test.GetType().GetProperty("FaceMode", BindingFlags.Public | BindingFlags.Instance);
                if (faceModeProperty?.PropertyType != typeof(EnumFaceMode) || !faceModeProperty.CanRead)
                {
                    return;
                }

                fields["Face_Mode"] = faceModeProperty.GetValue(test)?.ToString() ?? string.Empty;
            }

            private static string GetTestName(TestInfoBase test)
            {
                var nameProperty = test.GetType().GetProperty("Name");
                return nameProperty?.GetValue(test)?.ToString() ?? string.Empty;
            }

            private static List<TestTargetExportRow> GetTestTargets(TestInfoBase test)
            {
                var result = new List<TestTargetExportRow>();
                if (test?.TargetSetting?.EnabledTargetPoints == null)
                {
                    return result;
                }

                foreach (var target in test.TargetSetting.EnabledTargetPoints)
                {
                    if (target == null)
                    {
                        continue;
                    }

                    var fields = test.TargetSetting.TargetSamefields
                        ? test.TargetSetting.Fields?.ToList()
                        : target.Selectfields?.ToList();

                    if (fields == null || fields.Count == 0)
                    {
                        result.Add(new TestTargetExportRow
                        {
                            FieldX = null,
                            FieldY = null,
                            Target = target
                        });
                        continue;
                    }

                    foreach (var field in fields)
                    {
                        result.Add(new TestTargetExportRow
                        {
                            FieldX = field?.ColumnIndex,
                            FieldY = field?.RowIndex,
                            Target = target
                        });
                    }
                }

                return result;
            }

            private sealed class TestTargetExportRow
            {
                public int? FieldX { get; set; }
                public int? FieldY { get; set; }
                public TargetLocation Target { get; set; }
            }

            private static string GetTestTypeName(TestInfoBase test)
            {
                if (test == null)
                {
                    return string.Empty;
                }

                if (test is TestInfo_CD testCd)
                {
                    return Enum.GetName(typeof(EnumTargetType), testCd.TrainSetting_CD.Type) ?? string.Empty;
                }

                if (test is TestInfo_BiB testBib)
                {
                    return Enum.GetName(typeof(EnumTargetType), testBib.TrainSetting_BiB.InnerType) ?? string.Empty;
                }

                var text = test.GetTestType();
                if (Enum.TryParse<EnumTargetType>(text, true, out var enumType))
                {
                    return enumType.ToString();
                }

                if (int.TryParse(text, out var enumValue) && Enum.IsDefined(typeof(EnumTargetType), enumValue))
                {
                    return ((EnumTargetType)enumValue).ToString();
                }

                return text ?? string.Empty;
            }
        }

        private static class RecipeExcelContentParser
        {
            /// <summary>
            /// 将Excel内容解析为Recipe对象。
            /// 对于普通sheet按行直接导入；
            /// 对于Test_info，先按Recipe分组，再统一聚合处理，避免同一个Test/Target被重复拆散。
            /// </summary>
            /// <param name="contents">Excel导入后的内容集合</param>
            /// <returns>解析后的Recipe集合</returns>
            public static List<RecipeElementsInfo> Parse(IEnumerable<RecipeContent> contents)
            {
                var result = new Dictionary<string, RecipeElementsInfo>(StringComparer.OrdinalIgnoreCase);
                if (contents == null)
                {
                    return new List<RecipeElementsInfo>();
                }

                foreach (var recipeGroup in contents
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var recipe = new RecipeElementsInfo
                    {
                        Id = recipeGroup.Key,
                        Tests = new ObservableCollection<TestInfoBase>()
                    };

                    var firstContent = recipeGroup.FirstOrDefault();
                    if (firstContent?.Fields != null)
                    {
                        if (firstContent.Fields.TryGetValue("Recipe_Name", out var recipeName) && recipeName != null)
                        {
                            recipe.Name = recipeName.ToString();
                        }
                        else if (firstContent.Fields.TryGetValue("RecipeName", out var recipeName2) && recipeName2 != null)
                        {
                            recipe.Name = recipeName2.ToString();
                        }
                    }

                    foreach (var content in recipeGroup.Where(x =>
                                 x.Fields != null &&
                                 !string.Equals(x.RecipeType, RecipeExcelSheetNames.TestInfo, StringComparison.OrdinalIgnoreCase)))
                    {
                        switch (content.RecipeType)
                        {
                            case RecipeExcelSheetNames.WaferInfo:
                                recipe.WaferMap ??= new WaferMapInfo();
                                ExcelMapReflectionHelper.ImportObject(recipe.WaferMap, content.Fields);
                                break;

                            case RecipeExcelSheetNames.AlignmentInfo:
                                recipe.Alignment ??= new AlignmentInfo();
                                ExcelMapReflectionHelper.ImportObject(recipe.Alignment, content.Fields);
                                break;

                            case RecipeExcelSheetNames.RunParameterInfo:
                                recipe.RunParameter ??= new RunParameter();
                                ExcelMapReflectionHelper.ImportObject(recipe.RunParameter, content.Fields);
                                break;
                        }
                    }

                    var testContents = recipeGroup
                        .Where(x => x.Fields != null &&
                                    string.Equals(x.RecipeType, RecipeExcelSheetNames.TestInfo, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    ApplyTestRow(recipe, testContents);

                    result[recipe.Id] = recipe;
                }

                return result.Values.ToList();
            }

            /// <summary>
            /// 处理Test_info数据。
            /// 逻辑说明：
            /// 1. 先按Test_Name分组，确保同名Test只创建一次；
            /// 2. 再按Target_ID分组，确保同一个Target只创建一次；
            /// 3. 同一个Target_ID下多行Field_X/Field_Y合并到同一个Fields或Selectfields中；
            /// 4. R3坐标和Origin_Point取该Target分组的第一条记录。
            /// </summary>
            /// <param name="recipe">当前Recipe对象</param>
            /// <param name="testContents">当前Recipe下所有Test_info行</param>
            private static void ApplyTestRow(RecipeElementsInfo recipe, IEnumerable<RecipeContent> testContents)
            {
                if (recipe == null || testContents == null)
                {
                    return;
                }

                var testGroups = testContents
                    .Where(x => x.Fields != null)
                    .GroupBy(x => GetString(x.Fields, "Test_Name"), StringComparer.OrdinalIgnoreCase);

                foreach (var testGroup in testGroups)
                {
                    var testName = testGroup.Key;
                    if (string.IsNullOrWhiteSpace(testName))
                    {
                        continue;
                    }

                    var firstRow = testGroup.First();
                    var testType = GetEnumTargetType(firstRow.Fields);

                    var test = FindTestByName(recipe.Tests, testName);
                    if (test == null)
                    {
                        test = CreateTestByType(testName, testType, firstRow.Fields);
                        if (test == null)
                        {
                            continue;
                        }

                        recipe.Tests ??= new ObservableCollection<TestInfoBase>();
                        recipe.Tests.Add(test);
                    }
                    if (string.IsNullOrWhiteSpace(test.Id))
                    {
                        test.Id = Guid.NewGuid().ToString();
                    }
                    test.TargetSetting ??= new TargetSetting();
                    test.TargetSetting.TargetSamefields = GetBool(firstRow.Fields, "TargetSamefields", test.TargetSetting.TargetSamefields);

                    if (test.TargetSetting.TargetSamefields && ShouldForceTargetSamefieldsFalse(testGroup))
                    {
                        test.TargetSetting.TargetSamefields = false;
                        int targetCount = testGroup
                                          .Where(x => x?.Fields != null)
                                          .GroupBy(x => BuildTargetGroupKey(x.Fields), StringComparer.OrdinalIgnoreCase)
                                          .Count();
                        LogHelper.LogInfo($"ImportRecipeFromExcelContents: TargetSamefields was corrected to false automatically.RecipeId={{recipe?.Id}}," +
                                          $" TestName={{testName}}, TargetCount={{targetCount}}, Reason=Field_X/Field_Y are inconsistent across targets");
                    }

                    var targetGroups = testGroup
                                         .Where(x => x.Fields != null)
                                         .GroupBy(x => BuildTargetGroupKey(x.Fields),StringComparer.OrdinalIgnoreCase)
                                         .Where(g => !string.IsNullOrWhiteSpace(g.Key));

                    foreach (var targetGroup in targetGroups)
                    {
                        var firstTargetRow = targetGroup.First();
                        var firstFields = firstTargetRow.Fields;

                        var originPointText = GetString(firstFields, "Origin_Point");
                        var r3x = GetDouble(firstFields, "Target_Position(R3)_X(mm)");
                        var r3y = GetDouble(firstFields, "Target_Position(R3)_Y(mm)");

                        var originPoint = EnumR3OrigionPoint.LLC;
                        if (Enum.TryParse(originPointText, true, out EnumR3OrigionPoint parsedOriginPoint))
                        {
                            originPoint = parsedOriginPoint;
                        }

                        var target = test.TargetSetting.EnabledTargetPoints.FirstOrDefault(t =>
                            t != null &&
                            t.OrgionPoint == originPoint &&
                            t.R3Vector != null &&
                            t.R3Vector.X == r3x &&
                            t.R3Vector.Y == r3y);

                        if (target == null)
                        {
                            target = new TargetLocation
                            {
                                Id = Guid.NewGuid().ToString(),
                                OrgionPoint = originPoint,
                                R3Vector = new WcsPoint(r3x, r3y),
                            };

                            test.TargetSetting.TargetPoints2.Add(target);
                        }
                        else
                        {
                            target.OrgionPoint = originPoint;
                            target.R3Vector ??= new WcsPoint();
                            target.R3Vector.X = r3x;
                            target.R3Vector.Y = r3y;
                        }

                        foreach (var row in targetGroup)
                        {
                            if (!TryGetField(row.Fields, out var field))
                            {
                                continue;
                            }

                            if (test.TargetSetting.TargetSamefields)
                            {
                                if (!test.TargetSetting.Fields.Any(f => f != null &&
                                                    f.RowIndex == field.RowIndex &&
                                                    f.ColumnIndex == field.ColumnIndex))
                                {
                                    test.TargetSetting.Fields.Add(field);
                                }
                            }
                            else
                            {
                                if (!target.Selectfields.Any(f => f != null &&
                                              f.RowIndex == field.RowIndex &&
                                              f.ColumnIndex == field.ColumnIndex))
                                {
                                    target.Selectfields.Add(field);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// 检查是否存在同一个Test/Target下，Field_X/Field_Y不完全相同的情况，如果存在则强制TargetSamefields=false，避免数据丢失。
            /// </summary>
            /// <param name="testGroup"></param>
            /// <returns></returns>
            private static bool ShouldForceTargetSamefieldsFalse(IEnumerable<RecipeContent> testGroup)
            {
                if (testGroup == null)
                {
                    return false;
                }

                var targetFieldGroups = testGroup
                    .Where(x => x?.Fields != null)
                    .GroupBy(x => BuildTargetGroupKey(x.Fields), StringComparer.OrdinalIgnoreCase)
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .Select(g => g
                        .Select(x => BuildFieldKey(x.Fields))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList())
                    .ToList();

                if (targetFieldGroups.Count <= 1)
                {
                    return false;
                }

                var first = targetFieldGroups.First();
                foreach (var current in targetFieldGroups.Skip(1))
                {
                    if (first.Count != current.Count)
                    {
                        return true;
                    }

                    for (int i = 0; i < first.Count; i++)
                    {
                        if (!string.Equals(first[i], current[i], StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// 根据名称查找已存在的Test。
            /// </summary>
            /// <param name="tests">Test集合</param>
            /// <param name="testName">Test名称</param>
            /// <returns>匹配到的Test；未找到返回null</returns>
            private static TestInfoBase FindTestByName(IEnumerable<TestInfoBase> tests, string testName)
            {
                if (tests == null || string.IsNullOrWhiteSpace(testName))
                {
                    return null;
                }

                foreach (var test in tests)
                {
                    if (test == null)
                    {
                        continue;
                    }

                    var nameProperty = test.GetType().GetProperty("Name");
                    var currentName = nameProperty?.GetValue(test)?.ToString();
                    if (string.Equals(currentName, testName, StringComparison.OrdinalIgnoreCase))
                    {
                        return test;
                    }
                }

                return null;
            }
            private static string BuildFieldKey(IReadOnlyDictionary<string, object> fields)
            {
                if (fields == null)
                {
                    return string.Empty;
                }

                string fieldX = GetString(fields, "Field_X");
                string fieldY = GetString(fields, "Field_Y");

                if (string.IsNullOrWhiteSpace(fieldX) || string.IsNullOrWhiteSpace(fieldY))
                {
                    return string.Empty;
                }

                return $"{fieldX}|{fieldY}";
            }
            private static string BuildTargetGroupKey(IReadOnlyDictionary<string, object> fields)
            {
                if (fields == null)
                {
                    return string.Empty;
                }

                string originPoint = GetString(fields, "Origin_Point");
                string r3x = GetDouble(fields, "Target_Position(R3)_X(mm)").ToString("G17", CultureInfo.InvariantCulture);
                string r3y = GetDouble(fields, "Target_Position(R3)_Y(mm)").ToString("G17", CultureInfo.InvariantCulture);

                return $"{originPoint}|{r3x}|{r3y}";
            }
            private static TestInfoBase CreateTestByType(string testName,EnumTargetType testType,IReadOnlyDictionary<string, object> fields)
            {
                TestInfoBase test;
                if (SupportsFaceMode(testType) && TryGetFaceMode(fields, out var faceMode))
                {
                    test = new TestInfo40(testType, testType)
                    {
                        Name = testName,
                        FaceMode = faceMode
                    };
                }
                else
                {
                    test = TestInfoFactory.Build(testType, testName);
                }

                if (test == null)
                {
                    return null;
                }

                test.TargetSetting ??= new TargetSetting();
                return test;
            }

            private static bool SupportsFaceMode(EnumTargetType testType)
            {
                switch (testType)
                {
                    case EnumTargetType.Bar:
                    case EnumTargetType.Box:
                    case EnumTargetType.Frame:
                    case EnumTargetType.Circle:
                    case EnumTargetType.Cross:
                    case EnumTargetType.LAngle:
                    case EnumTargetType.LCV:
                        return true;
                    default:
                        return false;
                }
            }

            private static bool TryGetFaceMode( IReadOnlyDictionary<string, object> fields,out EnumFaceMode faceMode)
            {
                faceMode = EnumFaceMode.SingleFace;
                if (fields == null || !fields.TryGetValue("Face_Mode", out var value) || value == null)
                {
                    return false;
                }

                var text = value.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                if (Enum.TryParse(text, true, out faceMode) && Enum.IsDefined(typeof(EnumFaceMode), faceMode))
                {
                    return true;
                }

                if (int.TryParse(text, out var enumValue) && Enum.IsDefined(typeof(EnumFaceMode), enumValue))
                {
                    faceMode = (EnumFaceMode)enumValue;
                    return true;
                }

                return false;
            }

            private static EnumTargetType GetEnumTargetType(IReadOnlyDictionary<string, object> fields)
            {
                var text = GetString(fields, "Test_Type");
                if (string.IsNullOrWhiteSpace(text))
                {
                    return EnumTargetType.Bar;
                }

                if (Enum.TryParse<EnumTargetType>(text, true, out var result))
                {
                    return result;
                }

                if (int.TryParse(text, out var enumValue) && Enum.IsDefined(typeof(EnumTargetType), enumValue))
                {
                    return (EnumTargetType)enumValue;
                }

                return EnumTargetType.Bar;
            }

            /// <summary>
            /// 创建默认Test对象。
            /// 当前默认创建BiB类型，并初始化TargetSetting相关集合，
            /// 便于后续按Target_ID和Field信息继续填充。
            /// </summary>
            /// <param name="testName">Test名称</param>
            /// <returns>默认创建的Test对象</returns>
            private static TestInfoBase CreateDefaultTest(string testName)
            {
                var test = new TestInfo_BiB(EnumTargetType.Bar)
                {
                    Name = testName
                };

                test.TargetSetting ??= new TargetSetting();
                return test;
            }

            /// <summary>
            /// 从字段字典中读取字符串。
            /// </summary>
            /// <param name="fields">字段集合</param>
            /// <param name="key">字段名</param>
            /// <returns>字符串值；不存在时返回空字符串</returns>
            private static string GetString(IReadOnlyDictionary<string, object> fields, string key)
            {
                return fields.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
            }

            /// <summary>
            /// 从字段字典中读取浮点数。
            /// </summary>
            /// <param name="fields">字段集合</param>
            /// <param name="key">字段名</param>
            /// <returns>浮点值；不存在时返回0</returns>
            private static double GetDouble(IReadOnlyDictionary<string, object> fields, string key)
            {
                if (!fields.TryGetValue(key, out var value) || value == null)
                {
                    return 0d;
                }

                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            private static bool TryGetField(IReadOnlyDictionary<string, object> fields, out RowColumnInfo field)
            {
                field = null;

                if (fields == null)
                {
                    return false;
                }

                if (!fields.TryGetValue("Field_X", out var fieldXValue) || fieldXValue == null ||
                    !fields.TryGetValue("Field_Y", out var fieldYValue) || fieldYValue == null)
                {
                    return false;
                }

                var fieldXText = fieldXValue.ToString()?.Trim();
                var fieldYText = fieldYValue.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(fieldXText) || string.IsNullOrWhiteSpace(fieldYText))
                {
                    return false;
                }

                field = new RowColumnInfo(
                    Convert.ToInt32(fieldYValue, CultureInfo.InvariantCulture),
                    Convert.ToInt32(fieldXValue, CultureInfo.InvariantCulture));

                return true;
            }
        }

        private static class RecipeExcelSheetNames
        {
            public const string WaferInfo = "Wafer_info";
            public const string AlignmentInfo = "Alignment_info";
            public const string RunParameterInfo = "RunParameter_info";
            public const string TestInfo = "Test_info";
        }

        private static class ExcelMapReflectionHelper
        {
            public static IReadOnlyList<(PropertyInfo Property, ExcelMapAttribute Map)> GetMappedProperties(Type type)
            {
                if (type == null)
                {
                    return Array.Empty<(PropertyInfo Property, ExcelMapAttribute Map)>();
                }

                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => (Property: p, Map: p.GetCustomAttribute<ExcelMapAttribute>(true)))
                    .Where(x => x.Map != null)
                    .OrderBy(x => x.Map.Order)
                    .ToList();
            }

            public static Dictionary<string, object> ExportObject(object target)
            {
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (target == null)
                {
                    return result;
                }

                var properties = GetMappedProperties(target.GetType());
                foreach (var item in properties)
                {
                    if (!item.Map.CanExport)
                    {
                        continue;
                    }

                    var value = item.Property.GetValue(target);
                    result[item.Map.ColumnName] = ConvertToExcelValue(value);
                }

                return result;
            }

            public static void ImportObject(object target, IReadOnlyDictionary<string, object> values)
            {
                if (target == null || values == null)
                {
                    return;
                }

                var properties = GetMappedProperties(target.GetType());
                foreach (var item in properties)
                {
                    if (!item.Map.CanImport)
                    {
                        continue;
                    }

                    var hasValue = values.TryGetValue(item.Map.ColumnName, out var rawValue);

                    if (!hasValue || IsEmptyExcelValue(rawValue))
                    {
                        if (item.Map.ImportDefaultValue != null)
                        {
                            var defaultValue = item.Map.GetImportDefaultValue(item.Property.PropertyType);
                            item.Property.SetValue(target, defaultValue);
                        }

                        continue;
                    }

                    var converted = ConvertFromExcelValue(item.Property.PropertyType, rawValue);
                    item.Property.SetValue(target, converted);
                }
            }
            private static bool IsEmptyExcelValue(object value)
            {
                if (value == null || value == DBNull.Value)
                {
                    return true;
                }

                return string.IsNullOrWhiteSpace(value.ToString());
            }
            private static object ConvertToExcelValue(object value)
            {
                if (value == null)
                {
                    return null;
                }

                if (value is Enum enumValue)
                {
                    return ExcelEnumHelper.ToExcelValue(enumValue);
                }

                if (value is bool boolValue)
                {
                    return boolValue;
                }

                if (value is DateTime dateTimeValue)
                {
                    return dateTimeValue;
                }

                return value;
            }

            private static object ConvertFromExcelValue(Type targetType, object rawValue)
            {
                if (targetType == null)
                {
                    throw new ArgumentNullException(nameof(targetType));
                }

                if (rawValue == null)
                {
                    return null;
                }

                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                var text = rawValue.ToString()?.Trim();

                if (underlyingType == typeof(string))
                {
                    return text;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return Nullable.GetUnderlyingType(targetType) != null
                        ? null
                        : Activator.CreateInstance(underlyingType);
                }

                if (underlyingType.IsEnum)
                {
                    return ExcelEnumHelper.ParseEnum(underlyingType, rawValue);
                }

                if (underlyingType == typeof(bool))
                {
                    if (bool.TryParse(text, out var boolValue))
                    {
                        return boolValue;
                    }

                    if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "y", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "n", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    throw new InvalidCastException($"Cannot convert '{text}' to bool.");
                }

                if (underlyingType == typeof(int))
                {
                    return Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(double))
                {
                    return Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(float))
                {
                    return Convert.ToSingle(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(decimal))
                {
                    return Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(long))
                {
                    return Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(short))
                {
                    return Convert.ToInt16(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(byte))
                {
                    return Convert.ToByte(rawValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(DateTime))
                {
                    if (rawValue is DateTime dt)
                    {
                        return dt;
                    }

                    return Convert.ToDateTime(rawValue, CultureInfo.InvariantCulture);
                }

                return Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);
            }
        }

        private static bool GetBool(IReadOnlyDictionary<string, object> fields, string key, bool defaultValue = false)
        {
            if (!fields.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            var text = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return defaultValue;
            }

            if (bool.TryParse(text, out var boolValue))
            {
                return boolValue;
            }

            if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "y", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "n", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return defaultValue;
        }

        private static void CreateWaferMapAfterImport(RecipeElementsInfo recipeElements)
        {
            var waferMap = recipeElements?.WaferMap;
            if (waferMap == null)
            {
                return;
            }

            if (waferMap.FieldInfos != null && waferMap.FieldInfos.Count > 0)
            {
                return;
            }

            if (!WaferMapFieldGenerator.CanCreate(waferMap))
            {
                LogHelper.LogInfo($"Skip create wafer map after import. Invalid wafer map. RecipeId={recipeElements.Id}, WaferMap={waferMap}");
                return;
            }

            waferMap.FieldInfos = WaferMapFieldGenerator.CreateFields(waferMap);
            waferMap.ModuleState = EnumModuleState.SetUp;

            LogHelper.LogInfo($"Create wafer map after import succeeded. RecipeId={recipeElements.Id}, IsFieldCenter={waferMap.IsFieldCenter}, IsShiftedFields={waferMap.IsShiftedFields}, FieldCount={waferMap.FieldInfos.Count}");
        }
    }
}
