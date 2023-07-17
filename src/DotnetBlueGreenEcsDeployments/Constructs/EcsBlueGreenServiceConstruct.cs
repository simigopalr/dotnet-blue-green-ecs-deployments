using Amazon.CDK;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.EC2;
using ECS = Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using ECR = Amazon.CDK.AWS.ECR;
using Constructs;
using System;

namespace DotnetBlueGreenEcsDeployments.Constructs
{
    public class EcsBlueGreenServiceConstruct  : Construct
    {
        public EcsBlueGreenServiceConstruct(Construct scope, string id, string apiName, IRole ecsTaskRole, ECR.IRepository ecrRepo, IVpc vpc, ECS.ICluster cluster)
             : base(scope, id)
        {
            var logging = new ECS.AwsLogDriver(new ECS.AwsLogDriverProps {
                LogGroup = new LogGroup(this, "apiLogGroup", new LogGroupProps{
                    LogGroupName = String.Concat("/ecs/", apiName),
                    RemovalPolicy = RemovalPolicy.DESTROY
                    })
           });

           // Creating the task definition
            var taskDefinition = new ECS.FargateTaskDefinition(this, "apiTaskDefinition", new ECS.FargateTaskDefinitionProps{
                Family = apiName,
                Cpu = 256,
                MemoryLimitMiB = 1024,
                TaskRole = ecsTaskRole,
                ExecutionRole = ecsTaskRole
            });

            taskDefinition.AddContainer("apiContainer", new ECS.ContainerDefinitionProps{
                Image = ECS.ContainerImage.FromEcrRepository(ecrRepo, "latest")
            }).AddPortMappings( new ECS.PortMapping [] {
                new ECS.PortMapping {
                    ContainerPort = 80,
                    Protocol = ECS.Protocol.TCP
                }
            });

            // Creating an application load balancer, listener and two target groups for Blue/Green deployment
            var alb = new ApplicationLoadBalancer(this, "alb", new ApplicationLoadBalancerProps{
                Vpc = vpc,
                InternetFacing = true
            });
            var albProdListener = alb.AddListener("albProdListener", new ApplicationListenerProps{
                Port = 80
            });
            var albTestListener = alb.AddListener("albTestListener", new ApplicationListenerProps{
                Port = 8080
            });
            albProdListener.Connections.AllowDefaultPortFromAnyIpv4("Allow traffic from everywhere");
            albTestListener.Connections.AllowDefaultPortFromAnyIpv4("Allow traffic from everywhere");

            // Target group 1
            var blueTargetGroup = new ApplicationTargetGroup(this, "blueGroup", new ApplicationTargetGroupProps{
                Vpc = vpc,
                Protocol = ApplicationProtocol.HTTP,
                Port = 80,
                TargetType = TargetType.IP,
                HealthCheck = new HealthCheck {
                    Path = "/",
                    Timeout = Duration.Seconds(30),
                    Interval = Duration.Seconds(30),
                    HealthyHttpCodes = "200"
                }
            });

            // Target group 2
            var greenTargetGroup = new ApplicationTargetGroup(this, "greenGroup", new ApplicationTargetGroupProps{
                Vpc = vpc,
                Protocol = ApplicationProtocol.HTTP,
                Port = 80,
                TargetType = TargetType.IP,
                HealthCheck = new HealthCheck {
                    Path = "/",
                    Timeout = Duration.Seconds(30),
                    Interval = Duration.Seconds(30),
                    HealthyHttpCodes = "200"
                }
            });

            albProdListener.AddTargetGroups("blueTarget", new AddApplicationTargetGroupsProps {
                TargetGroups = new ApplicationTargetGroup [] {
                    blueTargetGroup
                }
            });

            albTestListener.AddTargetGroups("greenTarget", new AddApplicationTargetGroupsProps {
                TargetGroups = new ApplicationTargetGroup [] {
                    greenTargetGroup
                }
            });

            var ecsService = new ECS.FargateService(this, "ecsService", new ECS.FargateServiceProps {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                HealthCheckGracePeriod = Duration.Seconds(60),
                DesiredCount = 3,
                DeploymentController = new ECS.DeploymentController{
                    Type = ECS.DeploymentControllerType.CODE_DEPLOY
                },
                ServiceName = apiName
            });

            ecsService.Connections.AllowFrom(alb, Port.Tcp(80));
            ecsService.Connections.AllowFrom(alb, Port.Tcp(8080));
            ecsService.AttachToApplicationTargetGroup(blueTargetGroup);
        }

    }
}
