using Amazon.CDK;
using Constructs;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.CodeBuild;
using ECR = Amazon.CDK.AWS.ECR;
using CodeCommit = Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.IAM;

namespace DotnetBlueGreenEcsDeployments
{
    public class DotnetBlueGreenEcsDeploymentsStack : Stack
    {
        internal DotnetBlueGreenEcsDeploymentsStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            //import the cfnoutputs from container stack and retrieve the relevant details
            var codeRepo = CodeCommit.Repository.FromRepositoryName(this, "codeRepo", Fn.ImportValue("repositoryName"));
            var ecrRepo = ECR.Repository.FromRepositoryName(this, "ecrRepo", Fn.ImportValue("ecrRepoName"));
            var codeBuildProject = Project.FromProjectName(this, "codeBuild", Fn.ImportValue("codeBuildProjectName"));
            var ecsTaskRole = Role.FromRoleArn(this, "ecsTaskRole", Fn.ImportValue("ecsTaskRoleArn"));

            var codePipelineRole = new Role(this, "codePipelineRole", new RoleProps {
                AssumedBy = new ServicePrincipal("codepipeline.amazonaws.com")
            });
            
            var codePipelinePolicy = new PolicyStatement(new PolicyStatementProps {
                Effect = Effect.ALLOW,
                Actions = new [] {
                    "iam:PassRole",
                    "sts:AssumeRole",
                    "codecommit:Get*",
                    "codecommit:List*",
                    "codecommit:GitPull",
                    "codecommit:UploadArchive",
                    "codecommit:CancelUploadArchive",
                    "codebuild:BatchGetBuilds",
                    "codebuild:StartBuild",
                    "codedeploy:CreateDeployment",
                    "codedeploy:Get*",
                    "codedeploy:RegisterApplicationRevision",
                    "s3:Get*",
                    "s3:List*",
                    "s3:PutObject"
                },
                Resources = new [] { "*" }
             });

            codePipelineRole.AddToPolicy(codePipelinePolicy);

            var vpc = new Vpc(this, "ecsClusterVPC");
            
            var cluster = new Cluster(this, "ecsCluster", new ClusterProps {
                Vpc = vpc,
                ContainerInsights = true
            });
            
            
        }
    }
}
