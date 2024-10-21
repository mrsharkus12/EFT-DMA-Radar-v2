using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Timers;
using Offsets;

namespace eft_dma_radar
{
    #region Matrix
    public struct Matrix
    {
        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;

        public static Matrix Identity => new Matrix
        {
            M11 = 1f,
            M12 = 0f,
            M13 = 0f,
            M14 = 0f,
            M21 = 0f,
            M22 = 1f,
            M23 = 0f,
            M24 = 0f,
            M31 = 0f,
            M32 = 0f,
            M33 = 1f,
            M34 = 0f,
            M41 = 0f,
            M42 = 0f,
            M43 = 0f,
            M44 = 1f
        };

        public Matrix(
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public static Matrix operator *(Matrix left, Matrix right)
        {
            Matrix result = new Matrix();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float sum = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        sum += left[row, i] * right[i, col];
                    }
                    result[row, col] = sum;
                }
            }
            return result;
        }

        public float this[int row, int column]
        {
            get
            {
                return row switch
                {
                    0 => column switch { 0 => M11, 1 => M12, 2 => M13, 3 => M14, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    1 => column switch { 0 => M21, 1 => M22, 2 => M23, 3 => M24, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    2 => column switch { 0 => M31, 1 => M32, 2 => M33, 3 => M34, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    3 => column switch { 0 => M41, 1 => M42, 2 => M43, 3 => M44, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    _ => throw new ArgumentOutOfRangeException(nameof(row))
                };
            }
            set
            {
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0: M11 = value; break;
                            case 1: M12 = value; break;
                            case 2: M13 = value; break;
                            case 3: M14 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    case 1:
                        switch (column)
                        {
                            case 0: M21 = value; break;
                            case 1: M22 = value; break;
                            case 2: M23 = value; break;
                            case 3: M24 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    case 2:
                        switch (column)
                        {
                            case 0: M31 = value; break;
                            case 1: M32 = value; break;
                            case 2: M33 = value; break;
                            case 3: M34 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    case 3:
                        switch (column)
                        {
                            case 0: M41 = value; break;
                            case 1: M42 = value; break;
                            case 2: M43 = value; break;
                            case 3: M44 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(row));
                }
            }
        }

        public static Matrix Transpose(Matrix pM)
        {
            Matrix pOut = new Matrix();
            pOut.M11 = pM.M11;
            pOut.M12 = pM.M21;
            pOut.M13 = pM.M31;
            pOut.M14 = pM.M41;
            pOut.M21 = pM.M12;
            pOut.M22 = pM.M22;
            pOut.M23 = pM.M32;
            pOut.M24 = pM.M42;
            pOut.M31 = pM.M13;
            pOut.M32 = pM.M23;
            pOut.M33 = pM.M33;
            pOut.M34 = pM.M43;
            pOut.M41 = pM.M14;
            pOut.M42 = pM.M24;
            pOut.M43 = pM.M34;
            pOut.M44 = pM.M44;
            return pOut;
        }
    }

    #endregion
    
    #region Aimbot
    public class Aimbot
        {
            private Config _config;  // Declare _config
            private float _aimbotFOV;        // Field of View
            private float _aimbotMaxDistance; // Max Distance
            private int _aimbotKeybind;      // Keybind

            public Aimbot()
            {
                _config = Program.Config;  // Initialize _config from Program.Config
            }
        private Player udPlayer;
        bool bLastHeld;
        public static float Rad2Deg(float rad)
        {
            return rad * (180.0f / (float)Math.PI);
        }
        private static void NormalizeAngle(ref Vector2 angle)
        {
            var newX = angle.X switch
            {
                <= -180f => angle.X + 360f,
                > 180f => angle.X - 360f,
                _ => angle.X
            };

            var newY = angle.Y switch
            {
                > 90f => angle.Y - 180f,
                <= -90f => angle.Y + 180f,
                _ => angle.Y
            };

            angle = new Vector2(newX, newY);
        }

        public static Vector2 CalcAngle(Vector3 source, Vector3 destination)
        {
            Vector3 difference = source - destination;
            float length = difference.Length();
            Vector2 ret = new Vector2();

            ret.Y = (float)Math.Asin(difference.Y / length);
            ret.X = -(float)Math.Atan2(difference.X, -difference.Z);
            ret = new Vector2(ret.X * 57.29578f, ret.Y * 57.29578f);

            return ret;
        }

        private CameraManager _cameraManager
        {
            get => Memory.CameraManager;
        }
        private ReadOnlyDictionary<string, Player> AllPlayers
        {
            get => Memory.Players;
        }
        private bool InGame
        {
            get => Memory.InGame;
        }

        private PlayerManager playerManager
        {
            get => Memory.PlayerManager;
        }

        private Player LocalPlayer
        {
            get => Memory.LocalPlayer;
        }
        
        #region Getters
        public Vector3 GetFireportPos()
        {
            if (!this.InGame || Memory.InHideout)
            {
                MessageBox.Show("Not in game");
                return new Vector3();
            }
            ulong handscontainer = Memory.ReadPtrChain(playerManager._proceduralWeaponAnimation, new uint[] { ProceduralWeaponAnimation.FirearmContoller, FirearmController.Fireport, Fireport.To_TransfromInternal[0], Fireport.To_TransfromInternal[1] });
            Transform transform_fireport = new Transform(handscontainer);
            Vector3 pos = transform_fireport.GetPosition();
            return new Vector3(pos.X, pos.Z, pos.Y);
        }

        private float D3DXVec3Dot(Vector3 a, Vector3 b)
        {
            return (a.X * b.X +
                    a.Y * b.Y +
                    a.Z * b.Z);
        }

        private bool WorldToScreen(Vector3 _Enemy, out Vector2 _Screen)
        {
            _Screen = new Vector2(0, 0);

            Matrix viewMatrix = _cameraManager.ViewMatrix;
            Matrix temp = Matrix.Transpose(viewMatrix);

            Vector3 translationVector = new Vector3(temp.M41, temp.M42, temp.M43);
            Vector3 up = new Vector3(temp.M21, temp.M22, temp.M23);
            Vector3 right = new Vector3(temp.M11, temp.M12, temp.M13);

            float w = D3DXVec3Dot(translationVector, _Enemy) + temp.M44;

            if (w < 0.098f)
            {
                return false;
            }

            // Calculate screen coordinates
            float y = D3DXVec3Dot(up, _Enemy) + temp.M24;
            float x = D3DXVec3Dot(right, _Enemy) + temp.M14;

            _Screen.X = (1920f / 2f) * (1f + x / w);
            _Screen.Y = (1080f / 2f) * (1f - y / w);

            return true;
        }

        public Vector3 GetHead(Player player)
        {
            if (!this.InGame || Memory.InHideout || !player.IsAlive)
            {
                return new Vector3();
            }

            var boneMatrix = Memory.ReadPtrChain(player.PlayerBody, [0x28, 0x28, 0x10]);
            var pointer = Memory.ReadPtrChain(boneMatrix, [0x20 + ((uint)PlayerBones.HumanHead * 0x8), 0x10]);
            Transform transform_Head = new Transform(pointer, false);
            return transform_Head.GetPosition();
        }

        public bool GetHeadScr(Player player, out Vector2 screen, out Vector3 pos)
        {
            screen = new Vector2();
            pos = new Vector3();
            if (player.BoneTransforms != null && player.BoneTransforms.Count != 0 && !player.IsLocalPlayer && !player.IsFriendlyActive && player.IsAlive && player.IsActive && Vector3.Distance(player.Position, LocalPlayer.Position) < 100)
            {
                Vector3 getHead = GetHead(player);
                Vector3 HeadPos = new Vector3(getHead.X, getHead.Z, getHead.Y);
                if (WorldToScreen(HeadPos, out Vector2 scrpos))
                {
                    screen = scrpos;
                    pos = HeadPos;
                    return true;
                }
            }
            return false;
        }

        public bool GetBoneScr(Player player, PlayerBones bone, out Vector2 screen, out Vector3 pos)
        {
            screen = new Vector2();
            pos = new Vector3();

            if (player.BoneTransforms != null && player.BoneTransforms.Count != 0 && !player.IsLocalPlayer && !player.IsFriendlyActive && player.IsAlive && player.IsActive && Vector3.Distance(player.Position, LocalPlayer.Position) < _config.AimbotMaxDistance)
            {
                Vector3 getBonePos = GetBonePosition(player, bone);
                Vector3 BonePos = new Vector3(getBonePos.X, getBonePos.Z, getBonePos.Y);
                if (WorldToScreen(BonePos, out Vector2 scrpos))
                {
                    screen = scrpos;
                    pos = BonePos;
                    return true;
                }
            }
            return false;
        }

        public Vector3 GetBonePosition(Player player, PlayerBones bone)
        {
            if (!this.InGame || Memory.InHideout || !player.IsAlive)
            {
                return new Vector3();
            }

            var boneMatrix = Memory.ReadPtrChain(player.PlayerBody, [0x28, 0x28, 0x10]);
            var pointer = Memory.ReadPtrChain(boneMatrix, [0x20 + ((uint)bone * 0x8), 0x10]);

            Transform boneTransform = new Transform(pointer, false);
            return boneTransform.GetPosition();
        }
        #endregion

        // Updated HardLock to use GetBoneScr for targeting multiple bones
        public void HardLock()
        {
            _aimbotFOV = _config.AimbotFOV;
            _aimbotMaxDistance = _config.AimbotMaxDistance; // Max targeting distance
            // _aimbotKeybind = _config.AimbotKeybind;
            bool aimbotClosest = _config.AimbotClosest;

            // Check if the aimbot key is held down
            // bool bHeld = keyboard.IsKeyDown(_aimbotKeybind);
            bool bHeld = _config.AimBotLockOn;

            try
            {
                if (this.InGame && !Memory.InHideout && _cameraManager != null)
                {
                    // Filter active and alive players within max distance
                    var players = this.AllPlayers?.Select(x => x.Value)
                        .Where(x => x.IsActive && x.IsAlive && Vector3.Distance(x.Position, LocalPlayer.Position) < _config.AimbotMaxDistance)
                        .ToList();

                    if (players != null && players.Any())
                    {
                        this._cameraManager.GetViewMatrixAsync();
                        Vector3 cameraPos = GetFireportPos();

                        if (bHeld && bHeld == bLastHeld && udPlayer != null && udPlayer.IsAlive && udPlayer.IsActive)
                        {
                            // Existing target lock logic
                            Vector3 targetPos = GetClosestBoneScr(udPlayer, out Vector2 screenPos);
                            Vector2 rel = new Vector2(screenPos.X - (1920f / 2f), screenPos.Y - (1080f / 2f));
                            var distToCrosshair = Math.Sqrt((rel.X * rel.X) + (rel.Y * rel.Y));

                            if (distToCrosshair < _aimbotFOV)
                            {
                                Vector2 ang = CalcAngle(cameraPos, targetPos);
                                if (!float.IsNaN(ang.X) && !float.IsNaN(ang.Y))
                                {
                                    LocalPlayer.SetAimLockRotation(ang);
                                }
                            }
                        }
                        else if (bHeld && (bHeld != bLastHeld || udPlayer == null || !udPlayer.IsAlive || !udPlayer.IsActive))
                        {
                            // Start searching for valid targets within FOV
                            List<Player> validTargets = new List<Player>();

                            foreach (var player in players)
                            {
                                Vector3 targetPos = GetClosestBoneScr(player, out Vector2 screenPos);
                                Vector2 rel = new Vector2(screenPos.X - (1920f / 2f), screenPos.Y - (1080f / 2f));
                                var distToCrosshair = Math.Sqrt((rel.X * rel.X) + (rel.Y * rel.Y));

                                // Only consider players within FOV
                                if (distToCrosshair < _aimbotFOV && distToCrosshair > 2)
                                {
                                    validTargets.Add(player);
                                }
                            }

                            if (validTargets.Count > 1 && aimbotClosest)
                            {
                                // Multiple targets in FOV, prioritize the one closest to the player
                                Player closestPlayer = validTargets
                                    .OrderBy(player => Vector3.Distance(player.Position, LocalPlayer.Position))
                                    .FirstOrDefault();

                                if (closestPlayer != null)
                                {
                                    Vector3 closestBone = GetClosestBoneScr(closestPlayer, out Vector2 screenPos);
                                    Vector2 ang = CalcAngle(cameraPos, closestBone);

                                    if (!float.IsNaN(ang.X) && !float.IsNaN(ang.Y))
                                    {
                                        LocalPlayer.SetAimLockRotation(ang);
                                        udPlayer = closestPlayer;  // Lock onto closest target
                                    }
                                }
                            }
                            else if (validTargets.Count > 0)
                            {
                                // If only one target or AimbotClosest is off, target the closest one to the crosshair
                                Player closestToCrosshair = validTargets
                                    .OrderBy(player => 
                                    {
                                        GetClosestBoneScr(player, out Vector2 screenPos);
                                        return Vector2.Distance(screenPos, new Vector2(1920f / 2f, 1080f / 2f));
                                    })
                                    .FirstOrDefault();

                                if (closestToCrosshair != null)
                                {
                                    Vector3 closestBone = GetClosestBoneScr(closestToCrosshair, out Vector2 screenPos);
                                    Vector2 ang = CalcAngle(cameraPos, closestBone);

                                    if (!float.IsNaN(ang.X) && !float.IsNaN(ang.Y))
                                    {
                                        LocalPlayer.SetAimLockRotation(ang);
                                        udPlayer = closestToCrosshair;  // Lock onto the closest to the crosshair
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"ERROR -> Aimer botter -> {ex.Message}\nStackTrace:{ex.StackTrace}");
            }

            bLastHeld = bHeld;  // Update the held state for the next frame
        }

        public Vector3 GetClosestBoneScr(Player player, out Vector2 screenPos)
        {
            Vector3 closestBonePos = new Vector3();
            screenPos = new Vector2();
            double closestDistance = double.MaxValue;

            List<(bool, PlayerBones)> boneOptions = new List<(bool, PlayerBones)>
            {
                (_config.AimbotHead, PlayerBones.HumanHead),
                (_config.AimbotNeck, PlayerBones.HumanNeck),
                (_config.AimbotChest, PlayerBones.HumanSpine3),
                (_config.AimbotPelvis, PlayerBones.HumanPelvis),
                (_config.AimbotRightLeg, PlayerBones.HumanRCalf),
                (_config.AimbotLeftLeg, PlayerBones.HumanLCalf)
            };

            foreach (var (isEnabled, bone) in boneOptions)
            {
                if (!isEnabled) continue;

                if (GetBoneScr(player, bone, out Vector2 boneScreenPos, out Vector3 bonePos))
                {
                    float distanceToCenter = Vector2.Distance(boneScreenPos, new Vector2(1920f / 2f, 1080f / 2f));

                    if (distanceToCenter < closestDistance)
                    {
                        closestDistance = distanceToCenter;
                        closestBonePos = bonePos;
                        screenPos = boneScreenPos;
                    }
                }
            }
            return closestBonePos;
        }
    }
    #endregion
}