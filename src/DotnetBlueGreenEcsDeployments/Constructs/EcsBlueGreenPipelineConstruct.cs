using Amazon.CDK;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.CodeDeploy;
using Pipeline = Amazon.CDK.AWS.CodePipeline;
using PipelineActions = Amazon.CDK.AWS.CodePipeline.Actions;
using Constructs;
using System;

namespace DotnetBlueGreenEcsDeployments.Constructs
{
    public class EcsBlueGreenPipelineConstruct  : Construct
    {
        public EcsBlueGreenPipelineConstruct(
            Construct scope,
            string id,
            EcsBlueGreenServiceConstruct ecsBlueGreenService,
            IRole codePipelineRole,
            IBucket artifactsBucket,
            IRepository codeRepo,
            IProject codeBuildProject
        ) : base(scope, id)
        {

            var deploymentConfigName = new CfnParameter(this, "deploymentConfigName", new CfnParameterProps {
                Type = "String",
                Default = "CodeDeployDefault.ECSLinear10PercentEvery1Minutes",
                AllowedValues = new [] {
                    "CodeDeployDefault.ECSLinear10PercentEvery1Minutes",
                    "CodeDeployDefault.ECSLinear10PercentEvery3Minutes",
                    "CodeDeployDefault.ECSCanary10Percent5Minutes",
                    "CodeDeployDefault.ECSCanary10Percent15Minutes",
                    "CodeDeployDefault.ECSAllAtOnce"
                },
                Description = "Shifts x percentage of traffic every x minutes until all traffic is shifted",
            });

            var ecsApplication = new EcsApplication(this, "ecs-application", new EcsApplicationProps {
                ApplicationName = "ecsApplication"
            });

            
            var ecsBlueGreenDeploymentGroup = new EcsDeploymentGroup(this, "ecsDeploymentGroup", new EcsDeploymentGroupProps {
                DeploymentGroupName = "ecsDeployGrp",
                Service = ecsBlueGreenService.ecsService,
                Application = ecsApplication,
                BlueGreenDeploymentConfig = new EcsBlueGreenDeploymentConfig {
                    BlueTargetGroup = ecsBlueGreenService.blueTargetGroup,
                    GreenTargetGroup = ecsBlueGreenService.greenTargetGroup,
                    Listener = ecsBlueGreenService.albProdListener,
                    TestListener = ecsBlueGreenService.albTestListener
                },
                DeploymentConfig = EcsDeploymentConfig.FromEcsDeploymentConfigName(this, "ecsDeploymentConfig", deploymentConfigName.ValueAsString),
            });

            var sourceArtifact = new Pipeline.Artifact_("sourceArtifact");
            var buildArtifact = new Pipeline.Artifact_("buildArtifact");

            // Code Pipeline - CloudWatch trigger event is created by CDK
            var pipeline = new Pipeline.Pipeline(this, "ecsBlueGreen", new Pipeline.PipelineProps {
                Role = codePipelineRole,
                PipelineName = "ecs-bg-deploy",
                ArtifactBucket = artifactsBucket,
                Stages = new [] {
                    new Pipeline.StageProps {
                        StageName = "Source",
                        Actions = new [] {
                            new PipelineActions.CodeCommitSourceAction(new PipelineActions.CodeCommitSourceActionProps{
                                ActionName = "Source",
                                Repository = codeRepo,
                                Output = sourceArtifact,
                                Branch = "main"
                            })
                        }
                    },
                    new Pipeline.StageProps {
                        StageName = "Build",
                        Actions = new [] {
                            new PipelineActions.CodeBuildAction(new PipelineActions.CodeBuildActionProps{
                                ActionName = "Build",
                                Project = codeBuildProject,
                                Input = sourceArtifact,
                                Outputs = new [] {
                                    buildArtifact
                                }
                            })
                        }
                    },
                    new Pipeline.StageProps {
                        StageName = "Deploy",
                        Actions = new [] {
                            new PipelineActions.CodeDeployEcsDeployAction(new PipelineActions.CodeDeployEcsDeployActionProps{
                                ActionName = "Deploy",
                                DeploymentGroup = ecsBlueGreenDeploymentGroup,
                                AppSpecTemplateFile = new Pipeline.ArtifactPath_(buildArtifact, "appspec.yaml"),
                                TaskDefinitionTemplateFile = new Pipeline.ArtifactPath_(buildArtifact, "taskdef.json")
                            })
                        }
                    }
                }
            });
            pipeline.Node.AddDependency(ecsBlueGreenDeploymentGroup);

            new CfnOutput(this, "ecsBlueGreenLBDns", new CfnOutputProps {
                Description = "Load balancer DNS",
                ExportName = "ecsBlueGreenLBDns",
                Value = ecsBlueGreenService.alb.LoadBalancerDnsName
            });
            new CfnOutput(this, "blueGreenPipeline", new CfnOutputProps {
                Description = "Blue Green Deployment Pipeline",
                ExportName = "blueGreenPipeline",
                Value = pipeline.PipelineName
            });
        }
    }
}
