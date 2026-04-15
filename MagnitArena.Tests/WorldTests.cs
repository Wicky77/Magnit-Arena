using Xunit;
using MagnitArena.Model;
using Vector2 = MagnitArena.Model.Vector2;

namespace MagnitArena.Tests
{
    public class WorldTests
    {
        [Fact]
        public void Player_Move_Right_PositionIncreases()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            world.SetPlayer(player);
            player.Move(Direction.Right, 1);
            player.Update();
            Assert.True(player.Position.X > 0);
        }

        [Fact]
        public void WinCondition_AllBoxesInZones()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            var box = new Box { Position = new Vector2(5, 5) };
            world.SetPlayer(player);
            world.Boxes.Add(box);
            world.Zones.Add(new Vector2(5, 5));
            world.CheckCollisions();
            Assert.True(world.CheckWin());
        }

        [Fact]
        public void LevelLoader_LoadsThreeLevels()
        {
            var levels = LevelLoader.LoadAllLevels();
            Assert.Equal(3, levels.Count);
        }

        [Fact]
        public void World_LoadLevel_SetsCurrentLevel()
        {
            var world = new World();
            world.LoadLevel(0);
            Assert.Equal(1, world.CurrentLevel);
        }

        [Fact]
        public void Enemy_FallsInPit_IsRemoved()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            var enemy = new Enemy { Position = new Vector2(18, 7) };
            world.SetPlayer(player);
            world.Enemies.Add(enemy);
            world.Pits.Add(new Vector2(18, 7));
            world.CheckCollisions();
            Assert.True(enemy.IsRemoved);
        }

        [Fact]
        public void Magnet_Pull_BoxMovesTowardsPlayer()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(5, 5) };
            var box = new Box { Position = new Vector2(10, 5) };
            world.SetPlayer(player);
            world.Boxes.Add(box);
            world.ApplyMagnetForce(true);
            Assert.True(box.Position.X < 10);
        }

        [Fact]
        public void Magnet_Push_BoxMovesAwayFromPlayer()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(5, 5) };
            var box = new Box { Position = new Vector2(10, 5) };
            world.SetPlayer(player);
            world.Boxes.Add(box);
            world.ApplyMagnetForce(false);
            Assert.True(box.Position.X > 10);
        }

        [Fact]
        public void NextLevel_IncrementsCurrentLevel()
        {
            var world = new World();
            world.LoadLevel(0);
            world.State = GameState.Won;
            world.NextLevel();
            Assert.Equal(2, world.CurrentLevel);
        }
    }
}