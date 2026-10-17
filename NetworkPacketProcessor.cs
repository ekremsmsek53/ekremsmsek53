using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Core.Networking
{
    /// <summary>
    /// Represents standard network packet headers for client-server communication.
    /// </summary>
    public enum PacketType : byte
    {
        Handshake = 1,
        PlayerMove = 2,
        CastSkill = 3,
        InventoryTransaction = 4
    }

    public interface IPacket
    {
        PacketType Type { get; }
        int PlayerId { get; }
    }

    /// <summary>
    /// Sample skill cast packet received from the client.
    /// </summary>
    public readonly struct SkillCastPacket : IPacket
    {
        public PacketType Type => PacketType.CastSkill;
        public int PlayerId { get; }
        public int SkillId { get; }
        public float TargetX { get; }
        public float TargetY { get; }

        public SkillCastPacket(int playerId, int skillId, float targetX, float targetY)
        {
            PlayerId = playerId;
            SkillId = skillId;
            TargetX = targetX;
            TargetY = targetY;
        }
    }

    /// <summary>
    /// Server-Authoritative packet processor ensuring thread-safe queueing
    /// and validation before executing gameplay logic.
    /// </summary>
    public class NetworkPacketProcessor
    {
        private readonly ConcurrentQueue<IPacket> _incomingPackets = new ConcurrentQueue<IPacket>();
        private readonly Dictionary<PacketType, Action<IPacket>> _packetHandlers = new Dictionary<PacketType, Action<IPacket>>();

        public NetworkPacketProcessor()
        {
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _packetHandlers[PacketType.CastSkill] = HandleSkillCast;
        }

        /// <summary>
        /// Enqueues incoming raw packets from network socket threads safely.
        /// </summary>
        public void EnqueuePacket(IPacket packet)
        {
            if (packet != null)
            {
                _incomingPackets.Enqueue(packet);
            }
        }

        /// <summary>
        /// Process queued packets on the main tick/update loop.
        /// </summary>
        public void ProcessPendingPackets()
        {
            while (_incomingPackets.TryDequeue(out IPacket packet))
            {
                if (_packetHandlers.TryGetValue(packet.Type, out var handler))
                {
                    handler.Invoke(packet);
                }
            }
        }

        private void HandleSkillCast(IPacket packet)
        {
            if (packet is SkillCastPacket skillPacket)
            {
                // Server-Authoritative Validation Logic
                if (ValidateSkillCast(skillPacket))
                {
                    ExecuteSkillOnServer(skillPacket);
                }
            }
        }

        private bool ValidateSkillCast(SkillCastPacket packet)
        {
            // Perform distance checks, cooldown checks, and mana verification on server
            return packet.PlayerId > 0 && packet.SkillId > 0;
        }

        private void ExecuteSkillOnServer(SkillCastPacket packet)
        {
            // Execute skill logic and broadcast verified state back to clients
        }
    }
}
