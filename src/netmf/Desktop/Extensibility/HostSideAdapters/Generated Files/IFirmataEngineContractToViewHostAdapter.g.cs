//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PervasiveDigital.Scratch.DeploymentHelper.Extensibility.HostSideAdapters
{
    
    [System.AddIn.Pipeline.HostAdapterAttribute()]
    public class IFirmataEngineContractToViewHostAdapter : PervasiveDigital.Scratch.DeploymentHelper.Extensibility.IFirmataEngine
    {
        private PervasiveDigital.Scratch.DeploymentHelper.Extensibility.Contracts.IFirmataEngineContract _contract;
        private System.AddIn.Pipeline.ContractHandle _handle;
        static IFirmataEngineContractToViewHostAdapter()
        {
        }
        public IFirmataEngineContractToViewHostAdapter(PervasiveDigital.Scratch.DeploymentHelper.Extensibility.Contracts.IFirmataEngineContract contract)
        {
            _contract = contract;
            _handle = new System.AddIn.Pipeline.ContractHandle(contract);
        }
        internal PervasiveDigital.Scratch.DeploymentHelper.Extensibility.Contracts.IFirmataEngineContract GetSourceContract()
        {
            return _contract;
        }
    }
}
