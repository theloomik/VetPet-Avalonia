using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public static class StaffArchive
    {
        public const string ArchivedMarker = "__archived__";

        public static bool IsArchived(Staff? staff) =>
            staff != null && staff.WorkDays == ArchivedMarker;
    }
}
