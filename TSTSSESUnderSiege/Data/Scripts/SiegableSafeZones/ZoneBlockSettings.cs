using ProtoBuf;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ModAPI;
using VRageMath;

namespace SiegableSafeZones
{
    [ProtoContract]
    public class ZoneBlockSettings
    {
        [ProtoMember(1)] public float _currentCharge;
        [ProtoMember(2)] public bool _isSieging;
        [ProtoMember(3)] public long _jdSiegingId;
        [ProtoMember(4)] public long _zoneBlockEntityId;
        [ProtoMember(5)] public Vector3D _blockPos;
        [ProtoMember(6)] public bool _isActive;
        [ProtoMember(7)] public long _zoneBlockFactionId;
        [ProtoMember(8)] public string _zoneBlockFactionTag;
        [ProtoMember(9)] public long _playerSieging;
        [ProtoMember(10)] public string _zoneBlockOwnerName;
        [ProtoMember(11)] public string _detailInfo;
        [ProtoMember(12)] public bool _alerted;
        [ProtoMember(13)] public bool _siegeCompleted;
        [ProtoIgnore] public NonSerializedData _nsd;

        public ZoneBlockSettings()
        {
            _currentCharge = 0;
            _isSieging = false;
            _jdSiegingId = 0;
            _zoneBlockEntityId = 0;
            _blockPos = new Vector3D();
            _isActive = false;
            _nsd = new NonSerializedData();
            _zoneBlockFactionId = 0;
            _zoneBlockFactionTag = "";
            _playerSieging = 0;
            _zoneBlockOwnerName = "";
            _detailInfo = "";
            _alerted = false;
            _siegeCompleted = false;
        }

        public ZoneBlockSettings(long entityId)
        {
            _currentCharge = 0;
            _isSieging = false;
            _jdSiegingId = 0;
            _zoneBlockEntityId = entityId;
            _blockPos = new Vector3D();
            _isActive = false;
            _nsd = new NonSerializedData();
            _zoneBlockFactionId = 0;
            _zoneBlockFactionTag = "";
            _playerSieging = 0;
            _zoneBlockOwnerName = "";
            _detailInfo = "";
            _alerted = false;
            _siegeCompleted = false;

        }

        public bool SiegeCompleted
        {
            get { return _siegeCompleted; }
            set
            {
                _siegeCompleted = value;
                _nsd._sync = true;
            }
        }

        public bool Alerted
        {
            get { return _alerted; }
            set
            {
                _alerted = value;
                _nsd._sync = true;
            }
        }

        public string DetailInfo
        {
            get { return _detailInfo; }
            set
            {
                _detailInfo = value;
                _nsd._sync = true;
                Block.RefreshCustomInfo();
            }
        }

        public string ZoneBlockOwnerName
        {
            get { return _zoneBlockOwnerName; }
            set
            {
                _zoneBlockOwnerName = value;
                _nsd._sync = true;
            }
        }

        public long PlayerSieging
        {
            get { return _playerSieging; }
            set
            {
                _playerSieging = value;
                _nsd._sync = true;
            }
        }

        public string ZoneBlockFactionTag
        {
            get { return _zoneBlockFactionTag; }
            set
            {
                _zoneBlockFactionTag = value;
                _nsd._sync = true;
            }

        }

        public long ZoneBlockFactionId
        {
            get { return _zoneBlockFactionId; }
            set
            {
                _zoneBlockFactionId = value;
                _nsd._sync = true;
            }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                _nsd._sync = true;
            }
        }

        public float CurrentCharge
        {
            get { return _currentCharge; } 
            set
            {
                _currentCharge = value;
                _nsd._sync = true;
            }
        }

        public bool IsSieging
        {
            get { return _isSieging; }
            set
            {
                _isSieging = value;
                _nsd._sync = true;
            }
        }

        public long JDSiegingId
        {
            get { return _jdSiegingId;}
            set
            {
                _jdSiegingId = value;
                _nsd._sync = true;
            }
        }

        public long ZoneBlockEntityId
        {
            get { return _zoneBlockEntityId; }
            set
            {
                _zoneBlockEntityId = value;
                _nsd._sync = true;
            }
        }

        public Vector3D ZoneBlockPos
        {
            get { return _blockPos; }
            set
            {
                _blockPos = value;
                _nsd._sync = true;
            }
        }

        public IMySafeZoneBlock Block
        {
            get
            {
                if (_nsd._safeZoneBlock == null)
                {
                    IMyEntity ent;
                    if (!MyAPIGateway.Entities.TryGetEntityById(ZoneBlockEntityId, out ent)) return null;

                    IMySafeZoneBlock block = ent as IMySafeZoneBlock;
                    Block = block;
                    return block;
                }
                else
                    return _nsd._safeZoneBlock;
            }
            set { _nsd._safeZoneBlock = value; }
        }

        public IMyTerminalBlock JDBlock
        {
            get
            {
                if (_nsd._jdBlock == null)
                {
                    IMyEntity ent;
                    if (!MyAPIGateway.Entities.TryGetEntityById(JDSiegingId, out ent)) return null;

                    IMyTerminalBlock block = ent as IMyTerminalBlock;
                    JDBlock = block;
                    return block;
                }
                else
                    return _nsd._jdBlock;
            }
            set { _nsd._jdBlock = value; }
        }

        public NonSerializedData NSD
        {
            get { return _nsd; }
            set { _nsd = value; }
        }

        public bool Sync
        {
            get { return _nsd._sync; }
            set { _nsd._sync = value; }
        }    
    }

    public struct NonSerializedData
    {
        public IMyTerminalBlock _jdBlock;
        public IMySafeZoneBlock _safeZoneBlock;
        public bool _sync;
    }
}
