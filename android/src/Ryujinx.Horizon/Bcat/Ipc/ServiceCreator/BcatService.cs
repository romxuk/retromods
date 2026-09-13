using LibHac.Bcat;
using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Bcat.Types;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Bcat;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Bcat.Ipc
{
    partial class BcatService : IBcatService
    {
        public BcatService(BcatServicePermissionLevel permissionLevel) { }

        [CmifCommand(10100)]
        public Result RequestSyncDeliveryCache(out IDeliveryCacheProgressService deliveryCacheProgressService)
        {
            deliveryCacheProgressService = new DeliveryCacheProgressService();

            return Result.Success;
        }

        [CmifCommand(10101)]
        public Result RequestSyncDeliveryCacheWithDirectoryName(DirectoryName directoryName, out IDeliveryCacheProgressService deliveryCacheProgressService)
        {
            // Match the existing whole-cache stub. No remote content is downloaded.
            Logger.Stub?.PrintStub(LogClass.ServiceBcat);
            return RequestSyncDeliveryCache(out deliveryCacheProgressService);
        }

        [CmifCommand(10200)]
        public Result CancelSyncDeliveryCacheRequest()
        {
            // Stubbed sync requests complete immediately, so there is no pending work.
            Logger.Stub?.PrintStub(LogClass.ServiceBcat);
            return Result.Success;
        }
    }
}
