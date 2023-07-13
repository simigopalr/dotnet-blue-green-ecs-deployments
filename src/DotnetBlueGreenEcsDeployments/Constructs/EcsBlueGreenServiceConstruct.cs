using System.Collections.Generic;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using Protocol = Amazon.CDK.AWS.EC2.Protocol;

namespace DotnetBlueGreenEcsDeployments.Constructs
{
    public class EcsBlueGreenServiceConstruct : Construct
    {
        public EcsBlueGreenServiceConstruct(Construct scope, string id)
            : base(scope, id)
        {
           
        }
       
    }
}
