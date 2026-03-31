using Microsoft.VisualStudio.TestTools.UnitTesting;
using MagnetArena.Model;

namespace MagnetArena.Tests
{
    [TestClass]
    public class WorldTests
    {
        [TestMethod]
        public void Player_Move_Right_PositionIncreases()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            world.SetPlayer(player);

            player.Move(Direction.Right, 1.0);
            player.Update();

            Assert.IsTrue(player.Position.X > 0);
        }

        [TestMethod]
        public void Magnet_Pull_ObjectMovesCloser()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            var box = new Box { Position = new Vector2(10, 0) };

            world.SetPlayer(player);
            world.Boxes.Add(box);

            world.ApplyMagnetForce(true, 5.0);
            box.Update();

            var distance = (box.Position - player.Position).Length;
            Assert.IsTrue(distance < 10);
        }

        [TestMethod]
        public void Magnet_Push_ObjectMovesAway()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            var box = new Box { Position = new Vector2(5, 0) };

            world.SetPlayer(player);
            world.Boxes.Add(box);

            var initialDist = (box.Position - player.Position).Length;

            world.ApplyMagnetForce(false, 5.0);
            box.Update();

            var newDist = (box.Position - player.Position).Length;
            Assert.IsTrue(newDist > initialDist);
        }

        [TestMethod]
        public void Friction_ObjectEventuallyStops()
        {
            var box = new Box { Position = new Vector2(0, 0), Velocity = new Vector2(10, 10) };

            for (int i = 0; i < 50; i++) box.Update();

            Assert.AreEqual(0, box.Velocity.X, 0.01);
            Assert.AreEqual(0, box.Velocity.Y, 0.01);
        }

        [TestMethod]
        public void WinCondition_AllEnemiesInPits()
        {
            var world = new World();
            var player = new Player { Position = new Vector2(0, 0) };
            var enemy = new Enemy { Position = new Vector2(5, 5) };

            world.SetPlayer(player);
            world.Enemies.Add(enemy);
            world.Pits.Add(new Vector2(5, 5));

            world.CheckCollisions();
            Assert.IsTrue(enemy.IsRemoved);
            Assert.IsTrue(world.CheckWin());
        }
    }
}
