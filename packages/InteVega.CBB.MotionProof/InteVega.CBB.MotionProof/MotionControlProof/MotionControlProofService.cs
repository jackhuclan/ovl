using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.DependencyInjection;
using InteVega.CBB.MotionProof.Abstraction;
using Megarobo.Ovl.Service.IntfFn;
using Microsoft.Extensions.Logging;

namespace InteVega.CBB.MotionProof
{
    public partial class MotionControlProofService : ServicePlatBase, IMotionControlProofService
    {
        private readonly IServiceVaccum? _servVac;
        private readonly IAxisProofService _axisProofService;
        private readonly IAxisProofConfigProvider _axisProofConfigProvider;
        private ILogger<MotionControlProofService> _logger;
        public MotionControlProofService(IServiceProvider prod,ILoggerFactory loggerFactory) : base(prod, loggerFactory)
        {
            _servVac = _prod.GetService<IServiceVaccum>();
            _axisProofService = _prod.GetService<IAxisProofService>();
            _axisProofConfigProvider= _prod.GetService<IAxisProofConfigProvider>();
            _logger = loggerFactory.CreateLogger<MotionControlProofService>();

        }
    }
}
