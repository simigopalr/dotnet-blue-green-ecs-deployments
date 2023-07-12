using Amazon.CDK;
using Constructs;
using Amazon.CDK.AWS.IAM;

namespace DotnetBlueGreenEcsDeployments
{
    public class DotnetBlueGreenEcsDeploymentsStack : Stack
    {
        internal DotnetBlueGreenEcsDeploymentsStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var ecsTaskRole = new Role(this, "ecsTaskRoleForWorkshop", new RoleProps {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
            });
            
            ecsTaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"));
            
            var codeBuildRole = new Role(this, "codeBuildServiceRole", new RoleProps {
                AssumedBy = new ServicePrincipal("codebuild.amazonaws.com")
            });
            
            var policyStatement = new PolicyStatement(new PolicyStatementProps {
                Effect = Effect.ALLOW,
                Actions = new [] {
                    "ecr:GetAuthorizationToken",
                    "ecr:BatchCheckLayerAvailability",
                    "ecr:InitiateLayerUpload",
                    "ecr:UploadLayerPart",
                    "ecr:CompleteLayerUpload",
                    "ecr:PutImage",
                    "s3:Get*",
                    "s3:List*",
                    "s3:PutObject",
                    "secretsmanager:GetSecretValue"
                },
                Resources = new [] { "*" } 
            });
            
            codeBuildRole.AddToPolicy(policyStatement);
        }
    }
}
