﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Super_Pete_The_Pirate.Objects;
using Super_Pete_The_Pirate.Scenes;
using System;
using System.Collections.Generic;
using static Super_Pete_The_Pirate.Extensions.Utils;

namespace Super_Pete_The_Pirate.Characters
{
    class Boss : Enemy
    {
        //--------------------------------------------------
        // Attacks constants

        private const int Melee = 0;
        private const int Dash = 1;
        private const int Cannonballs = 2;

        //--------------------------------------------------
        // Max HP

        private const int MaxHP = 50;

        //--------------------------------------------------
        // HP HUD

        private Rectangle _hpBackRegion;
        private Rectangle _hpFullRegion;
        private Rectangle _hpHalfRegion;
        private Texture2D _hpSpritesheetTexture;
        private Vector2 _hpBackPosition;
        private Vector2 _hpSpritesPosition;

        //--------------------------------------------------
        // Direction

        private Direction _direction;

        //--------------------------------------------------
        // Dash

        private const float DashSpeed = 1000.0f;
        private Direction _dashDirection;
        private float _dashDelayTick;
        private float _dashDelayMaxTick;
        private int _dashCount;
        private bool _preparingDash;
        private bool _isDashing;
        
        //--------------------------------------------------
        // Max velocity

        private new float MaxMoveSpeed = 400.0f;

        //--------------------------------------------------
        // Requesting Shot

        private bool _requestingShot;
        public bool RequestingShot => _requestingShot;

        //--------------------------------------------------
        // Projectiles

        private List<GameProjectile> _projectiles;

        //----------------------//------------------------//

        public Boss(Texture2D texture) : base(texture)
        {
            _enemyType = EnemyType.Boss;

            // Stand
            CharacterSprite.CreateFrameList("stand", 150);
            CharacterSprite.AddCollider("stand", new Rectangle(15, 0, 70, 64));
            CharacterSprite.AddFrames("stand", new List<Rectangle>()
            {
                new Rectangle(0, 0, 96, 96),
                new Rectangle(96, 0, 96, 96),
                new Rectangle(192, 0, 96, 96),
                new Rectangle(288, 0, 96, 96)
            }, new int[] { 0, 0, 0, 0 }, new int[] { -32, -32, -32, -32 });

            // Melee Attack
            CharacterSprite.CreateFrameList("melee_attack", 120, false);
            CharacterSprite.AddCollider("melee_attack", new Rectangle(15, 0, 70, 64));
            CharacterSprite.AddFrames("melee_attack", new List<Rectangle>()
            {
                new Rectangle(0, 320, 96, 94),
                new Rectangle(96, 320, 128, 94),
                new Rectangle(224, 320, 128, 94),
                new Rectangle(352, 320, 128, 94),
                new Rectangle(480, 320, 128, 94)
            }, new int[] { 0, -32, -32, -32, -32 }, new int[] { -29, -29, -29, -29, -29 });

            // Dash preparation
            CharacterSprite.CreateFrameList("dash_preparation", 100, false);
            CharacterSprite.AddCollider("dash_preparation", new Rectangle(15, 0, 70, 64));
            CharacterSprite.AddFrames("dash_preparation", new List<Rectangle>()
            {
                new Rectangle(0, 96, 96, 96),
                new Rectangle(96, 96, 96, 96),
            }, new int[] { 0, 0 }, new int[] { -32, -32 });

            // Dash attack
            CharacterSprite.CreateFrameList("dash_attack", 40);
            CharacterSprite.AddCollider("dash_attack", new Rectangle(15, 0, 70, 64));
            CharacterSprite.AddFrames("dash_attack", new List<Rectangle>()
            {
                new Rectangle(192, 96, 128, 96),
                new Rectangle(320, 96, 128, 96),
                new Rectangle(448, 96, 128, 96),
                new Rectangle(572, 96, 128, 96)
            }, new int[] { -16, -16, -16, -16 }, new int[] { -32, -32, -32, -32 });

            // Preparation of Cannons
            CharacterSprite.CreateFrameList("cannonballs", 100, false);
            CharacterSprite.AddCollider("cannonballs", new Rectangle(15, 0, 70, 64));
            CharacterSprite.AddFrames("cannonballs", new List<Rectangle>()
            {
                new Rectangle(0, 192, 96, 128),
                new Rectangle(96, 192, 96, 128),
                new Rectangle(192, 192, 96, 128),
                new Rectangle(288, 192, 96, 128)
            }, new int[] { 0, 0, 0, 0 }, new int[] { -64, -64, -64, -64 });

            // Damage
            CharacterSprite.CreateFrameList("damage", 130);
            CharacterSprite.AddCollider("damage", new Rectangle(15, 0, 70, 64));
            CharacterSprite.AddFrames("damage", new List<Rectangle>()
            {
                new Rectangle(512, 0, 128, 96),
                new Rectangle(384, 0, 128, 96),
            }, new int[] { 0, 0 }, new int[] { -32, -32 });

            // Attacks setup
            _attackFrameList = new string[]
            {
                "melee_attack",
                "dash_attack",
                "cannonballs"
            };

            // Combat system init
            _hp = 50;
            _viewRangeSize = new Vector2(10, 74);
            _viewRangeOffset = new Vector2(0, -5);
            _damage = 2;
            _dashDelayMaxTick = 1000.0f;
            _dashDelayTick = _dashDelayMaxTick;

            // Direction init
            _direction = CharacterSprite.Effect == SpriteEffects.None ? Direction.Left : Direction.Right;

            // Projectiles init
            _projectiles = new List<GameProjectile>();

            // HP HUD init
            _hpBackRegion = new Rectangle(0, 0, 344, 18);
            _hpFullRegion = new Rectangle(344, 0, 12, 13);
            _hpHalfRegion = new Rectangle(356, 0, 12, 13);
            _hpBackPosition = new Vector2(18, 217);
            _hpSpritesPosition = _hpBackPosition + new Vector2(10, 3);
            _hpSpritesheetTexture = ImageManager.loadMisc("BossHPSpritesheet");

            CreateViewRange();
        }

        public override void RequestAttack(int type)
        {
            if (type == Dash)
            {
                _preparingDash = true;
                CharacterSprite.SetFrameList("dash_preparation");
            }
            else
            {
                if (type == Cannonballs)
                {
                    CreateCannonballs();
                }
                base.RequestAttack(type);
            }
        }

        private void CreateCannonballs()
        {
            var sceneMap = (SceneMap)SceneManager.Instance.GetCurrentScene();
            var positions = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 }, new int[] { 1, 2 }, new int[] { 2, 3 },
                new int[] { 0, 1, 2 }, new int[] { 0, 2, 3 }
            };
            var positionsY = new int[] { 72, 108, 144, 180 };
            var i = _rand.Next(HP < MaxHP * 0.5f ? 7 : 5);
            var i2 = positions[i];
            var y1 = positionsY[i2[0]];
            var y2 = positionsY[i2[1]];
            sceneMap.CreateProjectile("cannonball", new Vector2(360, y1), -7, 0, 1, ProjectileSubject.FromEnemy);
            sceneMap.CreateProjectile("cannonball", new Vector2(360, y2), -7, 0, 1, ProjectileSubject.FromEnemy);
            if (i > 4)
            {
                var y3 = positionsY[i2[2]];
                sceneMap.CreateProjectile("cannonball", new Vector2(360, y3), -7, 0, 1, ProjectileSubject.FromEnemy);
            }
        }

        public override void PlayerOnSight(Vector2 playerPosition)
        {
            if (IsFreeToAttack())
                RequestAttack(Melee);
        }

        public override void Update(GameTime gameTime)
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_dashDelayTick > 0f)
                _dashDelayTick -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_isDashing)
                _velocity.X = _dashDirection == Direction.Right ? DashSpeed * deltaTime : -DashSpeed * deltaTime;

            base.Update(gameTime);

            if (_isDashing && _dashDirection == Direction.Left && Position.X < 0)
            {
                Position = new Vector2(0, Position.Y);
                _isDashing = false;
                _velocity.X = 0;
                CharacterSprite.Effect = SpriteEffects.FlipHorizontally;
                _direction = Direction.Right;
                _dashDelayTick = _dashDelayMaxTick;
            }
            else if (_isDashing && _dashDirection == Direction.Right && Position.X > 288)
            {
                Position = new Vector2(288, Position.Y);
                _isDashing = false;
                _velocity.X = 0;
                CharacterSprite.Effect = SpriteEffects.None;
                _direction = Direction.Left;
                _dashDelayTick = _dashDelayMaxTick;
            }

            if (HP < MaxHP * 0.5f)
                _dashDelayMaxTick = 500.0f;

            UpdateSpriteEffect();
        }

        public override void UpdateAttack(GameTime gameTime)
        {
            if (IsFreeToAttack() && _dashDelayTick <= 0 && _dashCount < 2)
            {
                RequestAttack(Dash);
                _dashCount++;
            }
            else if (IsFreeToAttack() && _dashDelayTick <= 0)
            {
                RequestAttack(Cannonballs);
                _dashDelayTick = _dashDelayMaxTick;
                _dashCount = 0;
            }

            if (_preparingDash && CharacterSprite.Looped)
            {
                _preparingDash = false;
                _isDashing = true;
                if (Position.X >= SceneManager.Instance.VirtualSize.X / 2)
                    _dashDirection = Direction.Left;
                else
                    _dashDirection = Direction.Right;
            }

            if (_isAttacking && _attackType == Cannonballs && CharacterSprite.Looped)
            {
                _requestingShot = true;
                _dashDelayTick = _dashDelayMaxTick * 1.4f;
            }

            if (!_preparingDash && !_isDashing)
                base.UpdateAttack(gameTime);
        }

        public override void UpdateFrameList()
        {
            if (_dying)
                CharacterSprite.SetIfFrameListExists("dying");
            else if (_preparingDash)
                CharacterSprite.SetFrameList("dash_preparation");
            else if (_isDashing)
                CharacterSprite.SetFrameList("dash_attack");
            else if (_isAttacking)
                CharacterSprite.SetFrameList(_attackFrameList[_attackType]);
            else if (CharacterSprite.ImmunityAnimationActive)
                CharacterSprite.SetIfFrameListExists("damage");
            else if (!_isOnGround)
                CharacterSprite.SetIfFrameListExists("jumping");
            else
                CharacterSprite.SetFrameList("stand");
        }

        private void UpdateSpriteEffect()
        {
            if (_isDashing)
            {
                if (_velocity.X < 0 && CharacterSprite.Effect == SpriteEffects.FlipHorizontally)
                    CharacterSprite.Effect = SpriteEffects.None;
                else if (_velocity.X > 0 && CharacterSprite.Effect == SpriteEffects.None)
                    CharacterSprite.Effect = SpriteEffects.FlipHorizontally;
            }
            else
            {
                CharacterSprite.Effect = _direction == Direction.Left ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            }
        }

        public bool IsFreeToAttack()
        {
            return !_isDashing && !_preparingDash && !_isAttacking && !_requestAttack && !_requestErase;
        }

        protected override float GetMaxMoveSpeed()
        {
            return MaxMoveSpeed;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_hpSpritesheetTexture, _hpBackPosition, _hpBackRegion, Color.White);
            var halfHP = Math.Floor(HP / 2.0f);
            for (var i = 0; i < Math.Ceiling(HP / 2.0f); i++)
            {
                var region = i == halfHP ? _hpHalfRegion : _hpFullRegion;
                var position = _hpSpritesPosition + (region.Width * i + 1 * i) * Vector2.UnitX;
                spriteBatch.Draw(_hpSpritesheetTexture, position, region, Color.White);
            }
        }
    }
}
