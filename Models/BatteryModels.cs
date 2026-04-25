using System;

namespace Eternal.Models
{
    public record BatteryInfo(
        string Status,
        int ChargeLevel,
        string PowerSource,
        double WearLevel,
        int DesignCapacity,
        int FullChargeCapacity,
        int CurrentCapacity,
        int CycleCount,
        string Chemistry,
        string DeviceName
    );
}
