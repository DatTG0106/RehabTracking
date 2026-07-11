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
        public const string SessionSavedSuccessTemplate = "Buổi tập {0} phút đã được lưu thành công!";
        public const string SessionSavedError = "Buổi tập đã ghi nhận nhưng không thể lưu vào hệ thống.";
        public const string SessionTooShort = "Buổi tập quá ngắn (< 1 phút), không được lưu.";
        public const string SessionSaveException = "Lỗi khi lưu buổi tập. Vui lòng thử lại.";

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
        public const string AccountLocked = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin để biết thêm chi tiết.";
        public const string RequireLoginToCheckout = "Vui lòng đăng nhập để tiếp tục thanh toán.";
        
        // Register Messages
        public const string RegisterSuccess = "Đăng ký thành công! Vui lòng đăng nhập.";
        public const string EmailAlreadyExists = "Email này đã được đăng ký. Vui lòng dùng email khác hoặc đăng nhập.";
        
        // Treatment Plan Messages
        public const string PlanUpdateSuccess = "Phác đồ điều trị đã được cập nhật thành công!";
        public const string PlanUpdateFailed = "Không thể cập nhật phác đồ. Vui lòng thử lại.";
        public const string PlanUpdateError = "Lỗi hệ thống khi lưu phác đồ. Vui lòng thử lại.";

        // New Validation Messages
        public const string RequiredFullName = "Vui lòng nhập họ và tên.";
        public const string FullNameMaxLength = "Họ tên không được vượt quá 100 ký tự.";
        public const string RequiredEmail = "Vui lòng nhập email.";
        public const string RequiredPassword = "Vui lòng nhập mật khẩu.";
        public const string PasswordMinLength6 = "Mật khẩu phải có ít nhất 6 ký tự.";
        public const string RequiredConfirmPassword = "Vui lòng xác nhận mật khẩu.";

        public const string RangeRepetitions = "Số lần lặp mục tiêu phải từ 5 đến 100.";
        public const string RangeDuration = "Thời lượng mục tiêu phải từ 5 đến 300 phút.";
        public const string NoteMaxLength = "Ghi chú không được vượt quá 500 ký tự.";

        public const string RequiredLastName = "Vui lòng nhập họ.";
        public const string LastNameMaxLength = "Họ không được vượt quá 50 ký tự.";
        public const string RequiredFirstName = "Vui lòng nhập tên.";
        public const string FirstNameMaxLength = "Tên không được vượt quá 50 ký tự.";
        public const string RequiredPhone = "Vui lòng nhập số điện thoại.";
        public const string InvalidPhone = "Số điện thoại không hợp lệ (VD: 0912345678).";
        public const string RequiredAddress = "Vui lòng nhập địa chỉ giao hàng.";
        public const string AddressLength = "Địa chỉ phải có từ 10 đến 200 ký tự.";
        
        // Backend Exceptions
        public const string OrderNotFound = "Đơn hàng #{0} không tồn tại.";
        public const string OrderAlreadyProcessed = "Đơn hàng #{0} đã được xử lý (trạng thái: {1}). Không thể gán lại.";
        public const string SerialExists = "Mã Serial này đã tồn tại trong hệ thống. Vui lòng nhập mã khác.";
        public const string ProductNotFound = "Sản phẩm không tồn tại (ID: {0})";
        public const string OutOfStock = "Sản phẩm '{0}' không đủ số lượng trong kho (Còn lại: {1}).";
        public const string InvalidSerialLength = "Mã Serial hoặc địa chỉ MAC phải chứa ít nhất 6 ký tự.";
        public const string OrderProcessedSuccessTemplate = "Đơn #{0} đã được gán thiết bị [{1}] và chuyển trạng thái 'Đã giao' thành công!";
        public const string ConfirmToggleAccount = "Bạn có chắc chắn muốn {0} tài khoản {1}?";
        public const string ConfirmResetPassword = "Bạn có muốn đặt lại mật khẩu cho {0} về mặc định ({1})?";
        public const string ActionSuccess = "Thành công";
        public const string ActionError = "Lỗi";

        // Doctor Account Creation
        public const string DoctorNameRequired = "Vui lòng nhập họ tên bác sĩ.";
        public const string DoctorEmailRequired = "Vui lòng nhập địa chỉ email.";
        public const string DoctorEmailInvalid = "Địa chỉ email không đúng định dạng.";
        public const string DoctorNameTooShort = "Họ tên phải có ít nhất 3 ký tự.";
        public const string DoctorCreationValidationFailed = "Vui lòng kiểm tra lại thông tin trước khi tạo tài khoản.";

        // Cart / Access
        public const string CartNotAvailable = "Chức năng giỏ hàng chỉ dành cho bệnh nhân.";
        public const string AccessDeniedRole = "Bạn không có quyền truy cập trang này.";

        // Workout / Session
        public const string WorkoutPageTitle = "Bài tập hôm nay";
        public const string ExerciseCompleted = "Xuất sắc! Bạn đã hoàn thành bài tập.";
        public const string ExerciseInProgress = "Đang tập luyện...";
    }
}
