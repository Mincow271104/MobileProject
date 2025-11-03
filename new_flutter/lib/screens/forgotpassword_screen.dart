import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:ionicons/ionicons.dart';
import '../providers/user_provider.dart'; // Giả sử bạn có UserProvider, cần thêm các method mới vào đó

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  int _currentStep = 1;
  final _emailController = TextEditingController();
  final _verificationCodeController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  String _accountID = '';
  bool _isLoading = false;
  bool _showNewPassword = false;
  bool _showConfirmPassword = false;

  Future<void> _handleSendForgotToken() async {
    if (_emailController.text.isEmpty) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Vui lòng nhập email!')));
      return;
    }

    setState(() => _isLoading = true);
    final userProvider = Provider.of<UserProvider>(context, listen: false);
    final response = await userProvider.sendForgotToken(
      _emailController.text.trim(),
    );

    setState(() => _isLoading = false);

    if (response['success']) {
      setState(() {
        _accountID = response['accountID'];
        _currentStep = 2;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mã xác nhận đã được gửi đến email!')),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(response['message'] ?? 'Lỗi khi kiểm tra email!'),
        ),
      );
    }
  }

  Future<void> _handleVerifyForgotToken() async {
    if (_verificationCodeController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập mã xác nhận!')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final userProvider = Provider.of<UserProvider>(context, listen: false);
    final response = await userProvider.verifyForgotToken(
      _accountID,
      _verificationCodeController.text.trim(),
    );

    print('Verify Response: $response'); // Thêm dòng này để debug
    print(
      'AccountID sent: $_accountID',
    ); // Kiểm tra _accountID có giá trị không
    print(
      'Code sent: ${_verificationCodeController.text.trim()}',
    ); // Kiểm tra mã

    setState(() => _isLoading = false);

    if (response['success']) {
      setState(() => _currentStep = 3);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Xác nhận mã thành công!')));
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(response['message'] ?? 'Mã xác nhận không hợp lệ!'),
        ),
      );
    }
  }

  Future<void> _handleChangePassword() async {
    final newPassword = _newPasswordController.text.trim();
    final confirmPassword = _confirmPasswordController.text.trim();

    if (newPassword != confirmPassword) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mật khẩu xác nhận không khớp!')),
      );
      return;
    }

    final passwordRegex = RegExp(r'^[A-Za-z\d!@#$%^&*]{8,}$');
    if (!passwordRegex.hasMatch(newPassword)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mật khẩu mới phải có ít nhất 8 ký tự!')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final userProvider = Provider.of<UserProvider>(context, listen: false);
    final response = await userProvider.changeForgotPassword(
      _accountID,
      newPassword,
    );

    setState(() => _isLoading = false);

    if (response['success']) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Đổi mật khẩu thành công!')));
      await Future.delayed(const Duration(milliseconds: 500));
      Navigator.pushReplacementNamed(context, '/login');
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(response['message'] ?? 'Lỗi khi đổi mật khẩu!')),
      );
    }
  }

  Widget _buildForm() {
    switch (_currentStep) {
      case 1:
        return Column(
          children: [
            const Text(
              'Vui lòng nhập Email cần đổi mật khẩu:',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _emailController,
              decoration: InputDecoration(
                labelText: 'Email',
                prefixIcon: const Icon(
                  Ionicons.mail_outline,
                  color: Color.fromRGBO(91, 85, 85, 1),
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                focusedBorder: OutlineInputBorder(
                  borderSide: const BorderSide(
                    color: Color.fromRGBO(91, 85, 85, 1),
                    width: 2,
                  ),
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _isLoading ? null : _handleSendForgotToken,
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color.fromRGBO(91, 85, 85, 1),
                minimumSize: const Size(double.infinity, 50),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: Text(
                _isLoading ? 'Đang xử lý...' : 'Tiếp tục',
                style: const TextStyle(fontSize: 18, color: Colors.white),
              ),
            ),
          ],
        );
      case 2:
        return Column(
          children: [
            const Text(
              'Vui lòng nhập mã xác minh được gửi đến Email:',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _verificationCodeController,
              decoration: InputDecoration(
                labelText: 'Mã xác minh',
                prefixIcon: const Icon(
                  Ionicons.key_outline,
                  color: Color.fromRGBO(91, 85, 85, 1),
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                focusedBorder: OutlineInputBorder(
                  borderSide: const BorderSide(
                    color: Color.fromRGBO(91, 85, 85, 1),
                    width: 2,
                  ),
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _isLoading ? null : _handleVerifyForgotToken,
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color.fromRGBO(91, 85, 85, 1),
                minimumSize: const Size(double.infinity, 50),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: Text(
                _isLoading ? 'Đang xử lý...' : 'Tiếp tục',
                style: const TextStyle(fontSize: 18, color: Colors.white),
              ),
            ),
          ],
        );
      case 3:
        return Column(
          children: [
            const Text(
              'Nhập mật khẩu mới:',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _newPasswordController,
              obscureText: !_showNewPassword,
              decoration: InputDecoration(
                labelText: 'Mật khẩu mới',
                prefixIcon: const Icon(
                  Ionicons.lock_closed_outline,
                  color: Color.fromRGBO(91, 85, 85, 1),
                ),
                suffixIcon: IconButton(
                  icon: Icon(
                    _showNewPassword
                        ? Ionicons.eye_outline
                        : Ionicons.eye_off_outline,
                    color: const Color.fromRGBO(91, 85, 85, 1),
                  ),
                  onPressed: () =>
                      setState(() => _showNewPassword = !_showNewPassword),
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                focusedBorder: OutlineInputBorder(
                  borderSide: const BorderSide(
                    color: Color.fromRGBO(91, 85, 85, 1),
                    width: 2,
                  ),
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
            const SizedBox(height: 20),
            const Text(
              'Nhập lại mật khẩu:',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _confirmPasswordController,
              obscureText: !_showConfirmPassword,
              decoration: InputDecoration(
                labelText: 'Xác nhận mật khẩu',
                prefixIcon: const Icon(
                  Ionicons.lock_closed_outline,
                  color: Color.fromRGBO(91, 85, 85, 1),
                ),
                suffixIcon: IconButton(
                  icon: Icon(
                    _showConfirmPassword
                        ? Ionicons.eye_outline
                        : Ionicons.eye_off_outline,
                    color: const Color.fromRGBO(91, 85, 85, 1),
                  ),
                  onPressed: () => setState(
                    () => _showConfirmPassword = !_showConfirmPassword,
                  ),
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                focusedBorder: OutlineInputBorder(
                  borderSide: const BorderSide(
                    color: Color.fromRGBO(91, 85, 85, 1),
                    width: 2,
                  ),
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _isLoading ? null : _handleChangePassword,
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color.fromRGBO(91, 85, 85, 1),
                minimumSize: const Size(double.infinity, 50),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: Text(
                _isLoading ? 'Đang xử lý...' : 'Hoàn tất',
                style: const TextStyle(fontSize: 18, color: Colors.white),
              ),
            ),
          ],
        );
      default:
        return const SizedBox.shrink();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: _isLoading
            ? const CircularProgressIndicator(color: Color(0xFF4CAF50))
            : SingleChildScrollView(
                padding: const EdgeInsets.all(24.0),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.start,
                      children: [
                        IconButton(
                          icon: const Icon(Ionicons.arrow_back_outline),
                          onPressed: () =>
                              Navigator.pushReplacementNamed(context, '/login'),
                        ),
                        const Expanded(
                          child: Text(
                            'Quên Mật Khẩu',
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              fontSize: 24,
                              fontWeight: FontWeight.bold,
                              color: Color.fromRGBO(91, 85, 85, 1),
                            ),
                          ),
                        ),
                        const SizedBox(
                          width: 48,
                        ), // Để cân bằng với back button
                      ],
                    ),
                    const SizedBox(height: 40),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                      children: [
                        Column(
                          children: [
                            CircleAvatar(
                              radius: 20,
                              backgroundColor: _currentStep == 1
                                  ? const Color.fromRGBO(91, 85, 85, 1)
                                  : Colors.grey,
                              child: const Text(
                                '1',
                                style: TextStyle(color: Colors.white),
                              ),
                            ),
                            const Text('Nhập email'),
                          ],
                        ),
                        Column(
                          children: [
                            CircleAvatar(
                              radius: 20,
                              backgroundColor: _currentStep == 2
                                  ? const Color.fromRGBO(91, 85, 85, 1)
                                  : Colors.grey,
                              child: const Text(
                                '2',
                                style: TextStyle(color: Colors.white),
                              ),
                            ),
                            const Text('Xác minh mã'),
                          ],
                        ),
                        Column(
                          children: [
                            CircleAvatar(
                              radius: 20,
                              backgroundColor: _currentStep == 3
                                  ? const Color.fromRGBO(91, 85, 85, 1)
                                  : Colors.grey,
                              child: const Text(
                                '3',
                                style: TextStyle(color: Colors.white),
                              ),
                            ),
                            const Text('Đặt lại mật khẩu'),
                          ],
                        ),
                      ],
                    ),
                    const SizedBox(height: 40),
                    _buildForm(),
                  ],
                ),
              ),
      ),
    );
  }
}
