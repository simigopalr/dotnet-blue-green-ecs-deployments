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
            
            var ecrRepo = new  ECR.Repository(this, "ecrRepo", new ECR.RepositoryProps {
                ImageScanOnPush = true
            });
            
            var codeRepo = new CodeCommit.Repository(this, "codeRepo", new CodeCommit.RepositoryProps {
                RepositoryName = "nginx-sample"
            });
            
            new Project(this, "codeBuild", new ProjectProps {
                Role = codeBuildRole,
                Description = "Code build project for the application",
                Environment = new BuildEnvironment {
                    BuildImage = LinuxBuildImage.STANDARD_5_0,
                    ComputeType = ComputeType.SMALL,
                    Privileged = true,
                    EnvironmentVariables = new Dictionary<string, IBuildEnvironmentVariable> {
                        { "REPOSITORY_URI", new BuildEnvironmentVariable {
                                Value = ecrRepo.RepositoryUri,
                                Type = BuildEnvironmentVariableType.PLAINTEXT,
                            }
                        },
                        {  "TASK_EXECUTION_ARN", new BuildEnvironmentVariable {
                                Value = ecsTaskRole.RoleArn,
                                Type = BuildEnvironmentVariableType.PLAINTEXT,
                            } 
                        },
                    }
                },
                Source = Source.CodeCommit(new CodeCommitSourceProps { 
                    Repository = codeRepo,
                    BranchOrRef = "main",
                })
            });
        }
    }
}
