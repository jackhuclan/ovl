using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace InteVega.CBB.MotionProof
{
    public class ServicePlatBase
    {
        protected readonly IServiceProvider _prod;
        protected readonly IMotionControllerService? _motionController;
       protected ILogger<ServicePlatBase> _logger;

        public ServicePlatBase(IServiceProvider prod, ILoggerFactory loggerFactory) 
        {
            _prod = prod;
            _motionController = prod.GetService<IMotionControllerService>();
            _logger= loggerFactory.CreateLogger<ServicePlatBase>();
        }
        protected bool CanMoveZ(AxisType axisType)
        {
            if (string.IsNullOrEmpty(_motionController.GetAxisIdByType(axisType)))
            {
                _logger.LogInformation($"{axisType} don't exist. Can not Move");
                return false;
            }
            return true;
        }
    }
}
