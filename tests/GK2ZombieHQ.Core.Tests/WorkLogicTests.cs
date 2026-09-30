using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    [Collection("ZombieText")]
    public class WorkLogicTests
    {
        public WorkLogicTests() { ZombieText.Use("en"); }

        [Fact] public void Craft_shows_name_count_and_progress()
        {
            var w = new WorkInfo { CraftName = "Planks", OutputCount = 2, Progress = 0.456f };
            Assert.Equal("Planks ×2 · 45%", WorkLogic.Job(w));
        }

        [Fact] public void Single_output_has_no_count()
        {
            var w = new WorkInfo { CraftName = "Planks", OutputCount = 1, Progress = 0f };
            Assert.Equal("Planks · 0%", WorkLogic.Job(w));
        }

        [Fact] public void Problem_replaces_progress()
        {
            var w = new WorkInfo { CraftName = "Planks", OutputCount = 1, Progress = 0.3f, Problem = "job.NoResources" };
            Assert.Equal("Planks · no ingredients", WorkLogic.Job(w));
        }

        [Fact] public void Task_wins_over_craft()
        {
            var w = new WorkInfo { CraftName = "Planks", Task = "job.Planting" };
            Assert.Equal("planting", WorkLogic.Job(w));
        }

        [Fact] public void Nothing_known_means_idle()
        {
            Assert.Equal("waiting for work", WorkLogic.Job(new WorkInfo()));
        }

        [Fact] public void Passive_station_and_null_have_no_job()
        {
            Assert.Null(WorkLogic.Job(new WorkInfo { Passive = true, CraftName = "x" }));
            Assert.Null(WorkLogic.Job(null));
        }

        [Fact] public void Job_is_localized()
        {
            ZombieText.Use("ru");
            Assert.Equal("ждёт работы", WorkLogic.Job(new WorkInfo()));
            ZombieText.Use("en");
        }

        [Theory]
        [InlineData("OnStation", null)]
        [InlineData("GoToStation", "job.ToStation")]
        [InlineData("TeleportSeedsFromMultiInventory", "job.Planting")]
        [InlineData("GoToGardenBedToPlantSeeds", "job.Planting")]
        [InlineData("PlantingSeeds", "job.Planting")]
        [InlineData("GoToGardenBedToTakePlants", "job.Harvesting")]
        [InlineData("GatheringPlants", "job.Harvesting")]
        [InlineData("WaitingForWgoDeath", "job.Harvesting")]
        [InlineData("GoToInventoryToPickUpOrderItem", "job.Carrying")]
        [InlineData("PickingUpOrderItemFromInventory", "job.Carrying")]
        [InlineData("GoToStorageToPutItem", "job.Carrying")]
        [InlineData("WaitingOtherCaretakersOnInventory", "job.Waiting")]
        [InlineData("FailedToFindPath", "job.NoPath")]
        [InlineData("CanNotPutItemToInventory", "job.Full")]
        [InlineData("SomethingNew", null)]
        public void Task_keys(string state, string expected)
        {
            Assert.Equal(expected, WorkLogic.TaskKey(state));
        }

        [Theory]
        [InlineData("OK", null)]
        [InlineData("NotEnoughResources", "job.NoResources")]
        [InlineData("NotEnoughFuel", "job.NoFuel")]
        [InlineData("DoesntHaveRequiredTool", "job.NoTool")]
        [InlineData("NotEnoughSpaceInWgo", "job.Full")]
        public void Problem_keys(string status, string expected)
        {
            Assert.Equal(expected, WorkLogic.ProblemKey(status));
        }

        [Fact] public void All_job_keys_exist()
        {
            foreach (var s in new[] { "GoToStation", "PlantingSeeds", "GatheringPlants", "GoToStorageToPutItem", "WaitingOtherCaretakersOnInventory", "FailedToFindPath", "CanNotPutItemToInventory" })
                Assert.NotNull(ZombieText.Get(WorkLogic.TaskKey(s)));
            foreach (var s in new[] { "NotEnoughResources", "NotEnoughFuel", "DoesntHaveRequiredTool", "NotEnoughSpaceInWgo" })
                Assert.NotNull(ZombieText.Get(WorkLogic.ProblemKey(s)));
            Assert.NotNull(ZombieText.Get(WorkLogic.StationProblemKey("WaitingForWorkerPickUp")));
        }
    }
}
