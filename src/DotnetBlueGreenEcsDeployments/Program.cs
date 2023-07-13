using Amazon.CDK;
﻿using DotnetBlueGreenEcsDeployments.Stacks;

namespace DotnetBlueGreenEcsDeployments
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new DotnetBlueGreenEcsDeploymentsStack(app, "DotnetBlueGreenEcsDeploymentsStack");
            new DotnetContainerStack(app, "DotnetContainerStack");

            app.Synth();
        }
    }
}
