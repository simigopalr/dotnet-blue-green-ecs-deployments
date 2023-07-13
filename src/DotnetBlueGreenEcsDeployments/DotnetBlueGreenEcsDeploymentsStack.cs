using Amazon.CDK;
using Constructs;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.CodeBuild;
using ECR = Amazon.CDK.AWS.ECR;
using CodeCommit = Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.Pipelines;
using Amazon.CDK.AWS.S3;
using System.Collections.Generic;
using System;

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
            
           // var sourceArtifact = new CodePipeline.Artifact("sourceArtifact");
           // var buildArtifact = new CodePipeline.Artifact("buildArtifact");
    
            // S3 bucket for storing the code pipeline artifacts
            var artifactsBucket = new Bucket(this, "artifactsBucket", new BucketProps{
                Encryption = BucketEncryption.S3_MANAGED,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL
            });
            
            var denyUnEncryptedObjectUploads = new PolicyStatement(new PolicyStatementProps {
                Effect = Effect.DENY,
                Actions = new [] {"s3:PutObject"},
                Principals = new [] { new AnyPrincipal() },
                Resources = new [] { String.Concat( artifactsBucket.BucketArn, "/*") },
                Conditions = new Dictionary<string, object> {
                    { "StringNotEquals", new Dictionary<string, string> {{ "s3:x-amz-server-side-encryption", "aws:kms" }} }
                }
            });
            
            var denyInsecureConnections = new PolicyStatement(new PolicyStatementProps {
                Effect = Effect.DENY,
                Actions = new [] {"s3:*"},
                Principals = new [] { new AnyPrincipal() },
                Resources = new [] { String.Concat( artifactsBucket.BucketArn, "/*") },
                Conditions = new Dictionary<string, object> {
                    { "Bool", new Dictionary<string, string> {{ "aws:SecureTransport", "false" }} }
                }
            });
            
            artifactsBucket.AddToResourcePolicy(denyUnEncryptedObjectUploads);
            artifactsBucket.AddToResourcePolicy(denyInsecureConnections);
            
        }
    }
}
