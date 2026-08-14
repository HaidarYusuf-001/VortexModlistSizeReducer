namespace VortexModlistReducer.Core.Interfaces;

public interface IAnalyzerService
{
    bool ValidateDeploymentState(string stagingPath);
}