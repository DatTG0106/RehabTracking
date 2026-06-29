namespace RehabTracking.Web.Constants
{
    public static class AppMessages
    {
        // Common Messages
        public const string LoadingData = "Đang tải dữ liệu...";
        public const string ErrorOccurred = "Đã xảy ra lỗi. Vui lòng thử lại sau.";
        public const string SaveSuccess = "Lưu thành công!";
        public const string SaveFailed = "Lưu thất bại. Vui lòng kiểm tra lại.";

        // Dashboard Messages
        public const string WelcomePrefix = "Chào mừng, ";
        public const string NoDeviceActivated = "Bạn chưa kích hoạt thiết bị RehabBand. Hãy kích hoạt thiết bị để bắt đầu theo dõi tiến trình phục hồi của mình.";
        public const string ProgressGood = "Bạn đang tiến bộ tốt trong hành trình phục hồi chức năng.";
        public const string ProgressStartToday = "Hãy bắt đầu buổi tập đầu tiên của bạn hôm nay!";
        public const string NoDataYet = "Chưa có dữ liệu";
        public const string ClickToStart = "Nhấn \"Bắt đầu Tập luyện\" để xem biểu đồ EMG trực tiếp";
        public const string NoTreatmentPlan = "Chưa có phác đồ điều trị. Bác sĩ của bạn sẽ cập nhật sớm.";
        public const string NoHistory = "Chưa có lịch sử tập luyện. Hãy bắt đầu buổi tập đầu tiên!";

        // Session Messages
        public const string SessionSavedSuccessTemplate = "✅ Buổi tập {0} phút đã được lưu thành công!";
        public const string SessionSavedError = "⚠️ Buổi tập đã ghi nhận nhưng không thể lưu vào hệ thống.";
        public const string SessionTooShort = "ℹ️ Buổi tập quá ngắn (< 1 phút), không được lưu.";
        public const string SessionSaveException = "❌ Lỗi khi lưu buổi tập. Vui lòng thử lại.";

        // Cart & Checkout
        public const string CartEmpty = "Giỏ hàng trống";
        public const string CartEmptyDesc = "Bạn chưa thêm sản phẩm nào vào giỏ hàng.";
        public const string OrderSuccess = "Đặt hàng thành công!";
        public const string OrderFailed = "Lỗi khi xử lý đơn hàng. Vui lòng thử lại.";
        
        // Validation Messages
        public const string RequiredField = "Trường này là bắt buộc.";
        public const string InvalidEmail = "Địa chỉ email không hợp lệ.";
        public const string PasswordMinLength = "Mật khẩu phải có ít nhất {0} ký tự.";
        public const string PasswordsDoNotMatch = "Mật khẩu xác nhận không khớp.";
        public const string InvalidCredentials = "Sai thông tin đăng nhập.";
        
        // Backend Exceptions
        public const string OrderNotFound = "Đơn hàng #{0} không tồn tại.";
        public const string OrderAlreadyProcessed = "Đơn hàng #{0} đã được xử lý (trạng thái: {1}). Không thể gán lại.";
        public const string SerialExists = "Mã Serial này đã tồn tại trong hệ thống. Vui lòng nhập mã khác.";
        public const string ProductNotFound = "Sản phẩm không tồn tại (ID: {0})";
        public const string OutOfStock = "Sản phẩm '{0}' không đủ số lượng trong kho (Còn lại: {1}).";
    }
}
