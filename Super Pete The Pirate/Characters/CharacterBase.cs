﻿using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Super_Pete_The_Pirate
{
    class CharacterBase
    {
        //--------------------------------------------------
        // Character sprite

        public CharacterSprite CharacterSprite;

        //--------------------------------------------------
        // Physics variables

        protected float previousBottom;

        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }
        protected Vector2 _position;

        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }
        protected Vector2 _velocity;

        //--------------------------------------------------
        // Constants for controling horizontal movement

        protected const float MoveAcceleration = 13000.0f;
        protected const float MaxMoveSpeed = 1750.0f;
        protected const float GroundDragFactor = 0.48f;
        protected const float AirDragFactor = 0.58f;

        //--------------------------------------------------
        // Constants for controlling vertical movement

        protected const float MaxJumpTime = 0.35f;
        protected const float JumpLaunchVelocity = -2500.0f;
        protected const float GravityAcceleration = 3000.0f;
        protected const float MaxFallSpeed = 550.0f;
        protected const float JumpControlPower = 0.14f;
        protected const float PlayerSpeed = 0.3f;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return _isOnGround; }
        }
        protected bool _isOnGround;

        /// <summary>
        /// Current user movement input.
        /// </summary>
        protected float _movement;

        // Jumping state
        protected bool _isJumping;
        protected bool _wasJumping;
        protected float _jumpTime;

        protected Rectangle _localBounds;
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X) + _localBounds.X;
                int top = (int)Math.Round(Position.Y) + _localBounds.Y;
                return new Rectangle(left, top, CharacterSprite.GetFrameWidth(), CharacterSprite.GetFrameHeight());
            }
        }

        //--------------------------------------------------
        // Colliders

        private Texture2D _colliderTexture;

        //----------------------//------------------------//

        public CharacterBase(Texture2D texture)
        {
            CharacterSprite = new CharacterSprite(texture);

            _colliderTexture = new Texture2D(SceneManager.Instance.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _colliderTexture.SetData<Color>(new Color[] { Color.Red });

            // Calculate bounds within texture size.  
            int width = (int)(32);
            int left = (32 - width) / 2;
            int height = (int)(32);
            int top = 32 - height;
            _localBounds = new Rectangle(left, top, width, height);
        }

        public virtual void Update(GameTime gameTime)
        {
            ApplyPhysics(gameTime);

            _movement = 0.0f;
            _isJumping = false;

            UpdateSprite(gameTime);
        }

        private void UpdateSprite(GameTime gameTime)
        {
            if (Velocity.Y != 0)
                CharacterSprite.SetFrameList("jumping");
            else if (InputManager.Instace.KeyDown(Keys.Left) || InputManager.Instace.KeyDown(Keys.Right))
                CharacterSprite.SetFrameList("walking");
            else
                CharacterSprite.SetFrameList("stand");

            CharacterSprite.SetPosition(Position);
            CharacterSprite.Update(gameTime);
        }

        #region Collision

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            _velocity.X += _movement * MoveAcceleration * elapsed;
            _velocity.Y = MathHelper.Clamp(_velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

            _velocity.Y = DoJump(_velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                _velocity.X *= GroundDragFactor;
            else
                _velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            _velocity.X = MathHelper.Clamp(_velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity.
            Position += _velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                _velocity.X = 0;

            if (Position.Y == previousPosition.Y)
            {
                _velocity.Y = 0;
                _jumpTime = 0.0f;
            }
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (_isJumping)
            {
                // Begin or continue a jump
                if ((!_wasJumping && IsOnGround) || _jumpTime > 0.0f)
                {
                    _jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                }

                // If we are in the ascent of the jump
                if (0.0f < _jumpTime && _jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(_jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    _jumpTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                _jumpTime = 0.0f;
            }
            _wasJumping = _isJumping;

            return velocityY;
        }

        private void HandleCollisions()
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            var tileSize = GameMap.Instance.TileSize.X;
            int leftTile = (int)Math.Floor((float)bounds.Left / tileSize);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / tileSize)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / tileSize);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / tileSize)) - 1;

            // Reset flag to search for ground collision.
            _isOnGround = false;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    GameMap.TileCollision collision = GameMap.Instance.GetCollision(x, y);
                    if (collision != GameMap.TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = GameMap.Instance.GetTileBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == GameMap.TileCollision.Platform || (_velocity.X == 0))
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    _isOnGround = true;

                                // Ignore platforms, unless we are on the ground.
                                if (collision == GameMap.TileCollision.Block || IsOnGround)
                                {
                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == GameMap.TileCollision.Block) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }
        #endregion

        public void DrawCharacter(SpriteBatch spriteBatch)
        {
            CharacterSprite.Draw(spriteBatch, new Vector2(BoundingRectangle.X, BoundingRectangle.Y));
        }

        public void DrawColliderBox(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_colliderTexture, BoundingRectangle, Color.White * 0.5f);
        }
    }
}
