using Amazon.CDK;

namespace DotnetBlueGreenEcsDeployments
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new DotnetBlueGreenEcsDeploymentsStack(app, "DotnetBlueGreenEcsDeploymentsStack");

            app.Synth();
        }
    }
}
