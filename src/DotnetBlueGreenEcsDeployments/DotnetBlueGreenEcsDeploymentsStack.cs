using Amazon.CDK;
using Constructs;
using Amazon.CDK.AWS.IAM;
using ECR = Amazon.CDK.AWS.ECR;
using CodeCommit = Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.CodeBuild;
using System.Collections.Generic;

namespace DotnetBlueGreenEcsDeployments
{
    public class DotnetBlueGreenEcsDeploymentsStack : Stack
    {
        internal DotnetBlueGreenEcsDeploymentsStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            
        }
    }
}
