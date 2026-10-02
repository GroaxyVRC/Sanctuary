using System;
using System.Collections.Generic;
using System.Linq;

using Sanctuary.Packet;
using Sanctuary.Packet.Common;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Game.Helpers;

public static class WelcomeScreenHelper
{
    public const int PopularItemsGroupId = 6;
    private const int StationCashCurrencyType = 1;
    private const int PopularItemCount = 8;

    public static StoreBundleGroupDefinition GetPopularItems(
        StoreBundleGroupDefinition template, IEnumerable<AppStoreBundleDefinition> catalog,
        int count = PopularItemCount, int currencyType = StationCashCurrencyType)
    {
        // Sample without replacement from the SC bundles actually sent to this client.
        var candidates = catalog.Where(x => x.CurrencyType == currencyType && x.NameId > 0
            && x.DescriptionId > 0 && int.TryParse(x.Image.Image, out var imageId) && imageId > 0)
            .DistinctBy(x => x.Id).ToArray();
        Random.Shared.Shuffle(candidates);

        return new StoreBundleGroupDefinition
        {
            Id = template.Id,
            NameId = template.NameId,
            DescriptionId = template.DescriptionId,
            Status = template.Status,
            Image = template.Image,
            IsExclusive = template.IsExclusive,
            Entries = candidates.Take(Math.Max(0, count)).Select((bundle, index) =>
                new StoreBundleGroupDefinition.Entry
                {
                    StoreBundleId = bundle.Id,
                    DisplayOrder = index + 1
                }).ToDictionary(x => x.StoreBundleId)
        };
    }

    // Match the client's default marketplace banner, with a real announcement row
    // so its carousel initializes the count and selected index.
    public static AnnouncementDataSendPacket GetAnnouncementsPacket(IEnumerable<AnnouncementInfo>? announcements = null) => new()
    {
        Announcements = announcements?.ToList() ??
        [
            new()
            {
                Id = 1,
                Priority = 1,
                IconId = 38443,
                TitleStringId = 414,
                BodyStringId = 415,
                ButtonStringId = 436323,
                LuaCall = "Marketplace",
                Param2 = 31415
            }
        ]
    };
}
