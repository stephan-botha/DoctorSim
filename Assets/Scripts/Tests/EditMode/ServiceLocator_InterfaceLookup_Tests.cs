using NUnit.Framework;
using UnityEngine;
using MedMania.Core.Services;

public class ServiceLocator_InterfaceLookup_Tests
{
    private interface ITestService { }

    private sealed class TestServiceComponent : MonoBehaviour, ITestService { }

    [Test]
    public void Find_InterfaceService_ReturnsComponentImplementingInterface()
    {
        var go = new GameObject("TestService");
        try
        {
            var component = go.AddComponent<TestServiceComponent>();

            var service = ServiceLocator.Find<ITestService>();

            Assert.NotNull(service);
            Assert.AreSame(component, service);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
