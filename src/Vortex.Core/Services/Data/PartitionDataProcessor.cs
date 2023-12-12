﻿using MessagePack;
using Vortex.Core.Abstractions.Background;
using Vortex.Core.Abstractions.Services;
using Vortex.Core.Abstractions.Services.Data;
using Vortex.Core.Models.Configurations;
using Vortex.Core.Models.Data;
using Vortex.Core.Models.Entities.Addresses;
using Vortex.Core.Models.Entities.Entries;

namespace Vortex.Core.Services.Data
{
    public class PartitionDataProcessor : ParallelBackgroundQueueServiceBase<PartitionMessage>
    {
        private readonly IPartitionEntryService _partitionEntryService;
        private readonly Address _address;
        private readonly PartitionEntry _partitionEntry;
        private readonly IPartitionDataService<byte> _partitionDataService;
        private readonly NodeConfiguration _nodeConfiguration;

        int k = 0;

        private static TimeSpan GetPartitionEntityFlushPeriod(NodeConfiguration nodeConfiguration)
        {
            return new TimeSpan(0, 0, nodeConfiguration.BackgroundPositionEntry_FlushInterval);
        }

        public PartitionDataProcessor(IPartitionEntryService partitionEntryService,
            Address address,
            PartitionEntry partitionEntry,
            IPartitionDataService<byte> partitionDataService,
            NodeConfiguration nodeConfiguration) : base(address.Settings.PartitionSettings.PartitionThreadLimit, period: GetPartitionEntityFlushPeriod(nodeConfiguration))
        {
            _partitionEntryService = partitionEntryService;
            _address = address;
            _partitionEntry = partitionEntry;
            _partitionDataService = partitionDataService;
            _nodeConfiguration = nodeConfiguration;

            base.StartTimer();
        }

        public override void Handle(PartitionMessage request)
        {
            k++;
            // check if this message should go to other nodes in the cluster
            if (_partitionEntry.NodeOwner != _nodeConfiguration.NodeId)
            {
                TransmitMessageToNode(_partitionEntry.NodeOwner, request);

                return;
            }

            //prepare the message to store..
            _partitionEntry.CurrentEntry = _partitionEntry.CurrentEntry + 1;
            long entryId = _partitionEntry.CurrentEntry;

            // do breaking of indexes for each partition; this will enable new based on dateTime consumption.
            CoordinatePositionIndex(entryId, _partitionEntry);

            var messageToStore = new Message()
            {
                EntryId = entryId,
                MessageHeaders = request.MessageHeaders,
                MessageId = request.MessageId,
                MessagePayload = request.MessagePayload,
                HostApplication = request.HostApplication,
                SourceApplication = request.SourceApplication,
                SentDate = request.SentDate,
                StoredDate = request.StoredDate
            };

            byte[] entry = MessagePackSerializer.Serialize(entryId);
            byte[] message = MessagePackSerializer.Serialize(messageToStore);

            _partitionDataService.Put(entry, message);
        }

        public override void OnTimer_Callback(object? state)
        {
            Console.WriteLine($"REMOVE addressName:{_address.Name} partitionIndex:{_partitionEntry.PartitionId}: currentEntryPosition:{_partitionEntry.CurrentEntry}, countOfHandleMethod:{k}");

            // TODO: In case data is the same store the same state.

            // updating the Entry position for the partition.
            _partitionEntry.UpdatedAt = DateTime.UtcNow;
            _partitionEntry.UpdatedBy = "BACKGROUND_data_processor";
            _partitionEntryService.UpdatePartitionEntry(_partitionEntry);
        }

        private void CoordinatePositionIndex(long entryId, PartitionEntry partitionEntry)
        {
            var currentDate = DateTime.Now;
            string positionIndex = "";
            switch (partitionEntry.MessageIndexType)
            {
                case Models.Common.Addresses.MessageIndexTypes.HOURLY:
                    positionIndex = currentDate.ToString("yyyy-MM-dd HH");
                    break;
                case Models.Common.Addresses.MessageIndexTypes.DAILY:
                    positionIndex = currentDate.ToString("yyyy-MM-dd");
                    break;
                case Models.Common.Addresses.MessageIndexTypes.MONTHLY:
                    positionIndex = currentDate.ToString("yyyy-MM");
                    break;
                default:
                    positionIndex = currentDate.ToString("yyyy-MM-dd");
                    break;
            }

            if (partitionEntry.Positions.ContainsKey(positionIndex) != true)
            {
                // adding PositionEntry point
                partitionEntry.Positions.Add(positionIndex, new IndexPosition() { StartEntryPosition = entryId });
                var positionWithoutEndEntryPoint = partitionEntry.Positions.Where(x => x.Value.EndEntryPosition == null).FirstOrDefault().Key;
                partitionEntry.Positions[positionWithoutEndEntryPoint].EndEntryPosition = entryId - 1;
            }
        }

        private void TransmitMessageToNode(string nodeOwner, PartitionMessage request)
        {

        }
    }
}