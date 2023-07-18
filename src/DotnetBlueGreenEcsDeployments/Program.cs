using Amazon.CDK;
﻿using DotnetBlueGreenEcsDeployments.Stacks;

namespace DotnetBlueGreenEcsDeployments
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new EcsDeployStack(app, "EcsDeployStack");
            new ContainerStack(app, "ContainerStack");
            app.Synth();
        }
    }
}
