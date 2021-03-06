﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MAPAPI.Response;

namespace Poke1Protocol
{
    public enum SightAction
    {
        None,
        MoveToPlayer,
        PlayerToNPC
    }
    public class Npc
    {
        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public Guid Id { get; }
        public bool IsDoor => DoorNo > 0;
        public int DoorNo { get; } = 0;
        public string NpcName { get; }
        public int LosLength { get; private set; }
        public Direction Direction { get; private set; }
        public bool IsMoving => _path.Length > 0;
        public NPCData Data { get; }
        public bool IsBattler { get; private set; }
        public bool CanBattle { get; set; }

        private readonly string _path;

        public bool IsVisible { get; private set; }
        public SightAction SightAction { get; private set; }
        public Npc(NPCData data)
        {
            var en = data.Settings.Enabled;
            Data = data;
            PositionX = data.x;
            PositionY = -data.z;
            Id = data.ID;
            var result = SightAction.TryParse<SightAction>(data.Settings.SightAction.Replace(" ", ""), true, out var action);
            SightAction = result ? action : SightAction.None;
            _path = data.Settings.Path;
            UpdateLos(data.Settings.LOS);
            IsVisible = en;
#if DEBUG
            Console.WriteLine($"LOS: {LosLength}, Is Battler: {IsBattler}, Path = {_path}, Enabled: {en}");
            if (!en)
            {
                Console.WriteLine($"Not Enabled = ({PositionX}, {PositionY})");
            }
#endif
            if (data.Settings.Tags.ToLower() == "door_1")
            {
                DoorNo = 1;
            }
            else if (data.Settings.Tags.ToLower() == "door_2")
            {
                DoorNo = 2;
            }
            else if (data.Settings.Tags.ToLower() == "snorlax")
            {
                DoorNo = 3;
            }
            else if (data.Settings.Tags.ToLowerInvariant() == "whirlpool")
            {
                DoorNo = 3;
            }
            else if (data.Settings.Tags.ToLowerInvariant() == "door_3")
            {
                DoorNo = 4;
            }
            else if (data.Settings.Tags.ToLowerInvariant() == "door_4")
            {
                DoorNo = 5;
            }
            int num;
            if (data.Settings.NPCName.Length >= 2)
            {
                if (data.Settings.NPCName.Substring(0, 1) == "#")
                {
                    if (data.Settings.NPCName.Length >= 4)
                    {
                        if (!int.TryParse(data.Settings.NPCName.Substring(1, 3), out num))
                        {
                            num = 0;
                        }
                        if (num > 0 && num < 803)
                        {
                            NpcName = data.Settings.NPCName.Substring(4);
                        }
                    }
                    else
                    {
                        if (!int.TryParse(data.Settings.NPCName.Substring(1), out num))
                        {
                            num = 0;
                        }
                        if (num > 0 && num < 803)
                        {
                            NpcName = "";
                        }
                    }
                }
                if (data.Settings.NPCName.Length >= 2 && data.Settings.NPCName.Substring(0, 1) == "@")
                {
                    if (data.Settings.NPCName.Length >= 4)
                    {
                        if (!int.TryParse(data.Settings.NPCName.Substring(1, 3), out num))
                        {
                            num = 0;
                        }
                        if (num > 0 && num < 803)
                        {
                            data.Settings.NPCName = data.Settings.NPCName.Substring(4);
                        }
                    }
                    else
                    {
                        if (!int.TryParse(data.Settings.NPCName.Substring(1), out num))
                        {
                            num = 0;
                        }
                        if (num > 0 && num < 803)
                        {
                            data.Settings.NPCName = string.Empty;
                        }
                    }
                }
            }
            switch (data.Settings.Facing.ToLowerInvariant())
            {
                case "left":
                    Direction = Direction.Left;
                    break;
                case "right":
                    Direction = Direction.Right;
                    break;
                case "down":
                    Direction = Direction.Down;
                    break;
                case "up":
                    Direction = Direction.Up;
                    break;
                default:
                    Direction = Direction.None;
                    break;
            }
            NpcName = data.Settings.NPCName;
        }
        public Npc Clone()
        {
            var newData = Data;
            //copying latest updates to data..
            newData.x = PositionX;
            newData.z = -PositionY;
            newData.Settings.Facing = Direction.ToString().ToLowerInvariant();
            newData.Settings.LOS = LosLength;
            newData.Settings.Enabled = IsVisible;

            return new Npc(newData);
        }

        public Direction GetDriectionFrom(int x, int y)
        {
            if (x == PositionX && y > PositionY)
                return Direction.Up;
            if (x == PositionX && y < PositionY)
                return Direction.Down;
            if (y == PositionY && x > PositionX)
                return Direction.Left;
            if (y == PositionY && x < PositionX)
                return Direction.Right;

            return Direction.None;
        }

        public bool IsInLineOfSight(int x, int y)
        {
            if (x != PositionX && y != PositionY) return false;
            var distance = GameClient.DistanceBetween(PositionX, PositionY, x, y);
            if (distance > LosLength) return false;
            switch (Direction)
            {
                case Direction.Up:
                    return x == PositionX && y < PositionY;
                case Direction.Down:
                    return x == PositionX && y > PositionY;
                case Direction.Left:
                    return x < PositionX && y == PositionY;
                case Direction.Right:
                    return x > PositionX && y == PositionY;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ProcessActions(string actions)
        {
            var dir = Direction;
            var x = PositionX;
            var y = PositionY;
            DirectionExtensions.ApplyToDirectionFromChar(ref dir, actions, ref x, ref y);
            PositionX = x;
            PositionY = y;
            Direction = dir;
        }

        public void UpdateLos(int los)
        {
            LosLength = los;
            IsBattler = LosLength > 0 && (Data.Settings.SightAction == "Move To Player"
                || Data.Settings.SightAction == "Player To NPC");
            CanBattle = IsBattler;
        }

        public void SetVisibility(bool hide)
        {
            IsVisible = !hide;
            IsBattler = IsVisible && LosLength > 0 && (Data.Settings.SightAction == "Move To Player"
                || Data.Settings.SightAction == "Player To NPC");
            CanBattle = IsBattler;
        }
    }
}
