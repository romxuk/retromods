using LibHac.Bcat;
using NUnit.Framework;
using Ryujinx.Horizon;
using Ryujinx.Horizon.Bcat.Ipc.Types;
using Ryujinx.Horizon.Common;
using System;
using System.Linq;

namespace Ryujinx.Tests.HLE
{
    public class BcatCompatibilityTests
    {
        [Test]
        public void DirectorySyncCommandReturnsCompletedProgressInsteadOfThrowing()
        {
            // Exercise the actual service without opening kernel handles or downloading content.
            Type serviceType = typeof(ServiceTable).Assembly.GetType("Ryujinx.Horizon.Bcat.Ipc.BcatService", true);
            var constructor = serviceType.GetConstructors().Single();
            object permission = Enum.ToObject(constructor.GetParameters()[0].ParameterType, 0);
            object service = constructor.Invoke(new[] { permission });
            var method = serviceType.GetMethod("RequestSyncDeliveryCacheWithDirectoryName");
            Assert.That(method, Is.Not.Null);
            var attribute = method.GetCustomAttributesData().Single(a => a.AttributeType.Name == "CmifCommandAttribute");
            Assert.That(attribute.ConstructorArguments[0].Value, Is.EqualTo(10101U));

            object[] arguments = { default(DirectoryName), null };
            Assert.That(method.Invoke(service, arguments), Is.EqualTo(Result.Success));
            Assert.That(arguments[1], Is.Not.Null);
            using IDisposable progress = (IDisposable)arguments[1];
            object[] output = { null };
            Assert.That(progress.GetType().GetMethod("GetImpl").Invoke(progress, output), Is.EqualTo(Result.Success));
            var state = (DeliveryCacheProgressImpl)output[0];
            Assert.That(state.State, Is.EqualTo(DeliveryCacheProgressImpl.Status.Done));
            Assert.That(state.Result, Is.Zero);
            Assert.That(serviceType.GetMethod("CancelSyncDeliveryCacheRequest").Invoke(service, null), Is.EqualTo(Result.Success));
        }
    }
}
