using LivingBank.Api.Domain.Entities;

namespace LivingBank.Tests;

public class SyncScheduleTests
{
    [Fact]
    public void Default_Times_Are_6_12_18_23_Utc()
    {
        var schedule = new SyncSchedule();
        Assert.Equal(
            new[] { new TimeOnly(6, 0), new TimeOnly(12, 0), new TimeOnly(18, 0), new TimeOnly(23, 0) },
            schedule.Times);
    }

    [Fact]
    public void Times_Reflects_Custom_Values_In_Order()
    {
        var schedule = new SyncSchedule
        {
            Time1 = new TimeOnly(1, 0),
            Time2 = new TimeOnly(7, 30),
            Time3 = new TimeOnly(13, 15),
            Time4 = new TimeOnly(20, 45)
        };

        Assert.Equal(
            new[] { new TimeOnly(1, 0), new TimeOnly(7, 30), new TimeOnly(13, 15), new TimeOnly(20, 45) },
            schedule.Times);
    }

    [Fact]
    public void Times_Always_Has_Four_Slots()
    {
        Assert.Equal(4, new SyncSchedule().Times.Length);
    }
}
