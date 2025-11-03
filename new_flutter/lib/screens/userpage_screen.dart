import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:image_picker/image_picker.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'dart:io';
import 'package:dio/dio.dart';
import 'dart:convert'; // Thêm import này để sử dụng jsonDecode
import '../utils/api.dart';
import '../providers/user_provider.dart';
import '../models/user_model.dart';
import 'viewinvoice_screen.dart';
// Import trang mới để xem danh sách hóa đơn
import 'package:ionicons/ionicons.dart';

class UserpageScreen extends StatefulWidget {
  const UserpageScreen({super.key});

  @override
  State<UserpageScreen> createState() => _UserpageScreenState();
}

class _UserpageScreenState extends State<UserpageScreen> {
  UserModel? _userInfo;
  bool _isLoading = true;
  String _editField = '';
  String _tempValue = '';
  File? _imageFile;
  String _userImage = '';
  List<Map<String, dynamic>> _genders = [];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;

    // Load genders from AllCodes (Type = 'Gender')
    final gendersResponse = await apiService.getAllCodes('Gender');
    if (gendersResponse['errCode'] == 0 && gendersResponse['data'] != null) {
      _genders = List<Map<String, dynamic>>.from(gendersResponse['data']);
    }

    // Load user info
    final response = await apiService.getAccountInfo(user?.accountId ?? '');
    if (response['errCode'] == 0 && response['data'] != null) {
      setState(() {
        _userInfo = UserModel.fromJson(response['data']);
        _userImage = _userInfo!.userImage;
        _isLoading = false;
      });
    } else {
      Fluttertoast.showToast(msg: 'Lỗi tải thông tin người dùng');
      setState(() => _isLoading = false);
    }
  }

  void _editClick(String field) {
    setState(() {
      _editField = field;
      _tempValue = _getFieldValue(field);
    });
  }

  String _getFieldValue(String field) {
    switch (field) {
      case 'accountname':
        return _userInfo!.accountName;
      case 'username':
        return _userInfo!.userName;
      case 'phone':
        return _userInfo!.phone;
      case 'address':
        return _userInfo!.address;
      case 'gender':
        return _userInfo!.gender;
      default:
        return '';
    }
  }

  void _cancelEdit() {
    setState(() => _editField = '');
  }

  Future<void> _saveChanges() async {
    if (_editField.isEmpty && _imageFile == null) return;

    setState(() => _isLoading = true);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;

    Map<String, dynamic> updateData = {'accountid': user?.accountId};

    if (_editField.isNotEmpty) {
      updateData[_editField] = _tempValue;
    }

    if (_imageFile != null) {
      print('Uploading image...');
      final uploadResponse = await uploadImageToBackend(_imageFile!);
      print('Upload response: $uploadResponse');
      if (uploadResponse['errCode'] == 0 &&
          uploadResponse['data'] != null &&
          uploadResponse['data'].length > 0) {
        // Gửi object đầy đủ match React/backend
        updateData['userimage'] = uploadResponse['data'][0];
      } else {
        Fluttertoast.showToast(
          msg:
              'Tải ảnh thất bại: ${uploadResponse['errMessage'] ?? 'Lỗi không xác định'}',
        );
        setState(() => _isLoading = false);
        return;
      }
    }

    final response = await apiService.changeAccountInfo(updateData);
    setState(() => _isLoading = false);
    if (response['errCode'] == 0) {
      Fluttertoast.showToast(msg: 'Cập nhật thành công');
      await _loadData();
      setState(() => _editField = '');
      _imageFile = null;
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Cập nhật thất bại',
        backgroundColor: Colors.red,
      );
    }
  }

  Future<Map<String, dynamic>> uploadImageToBackend(File imageFile) async {
    try {
      final dio = Dio();
      dio.options.headers = {'Content-Type': 'multipart/form-data'};

      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          imageFile.path,
          filename: imageFile.path.split('/').last,
        ),
        'upload_preset': 'ThatCook',
      });

      final response = await dio.post(
        'https://api.cloudinary.com/v1_1/dqblg6ont/image/upload',
        data: formData,
      );

      print('Cloudinary response: ${response.data}');

      if (response.statusCode == 200) {
        // Trả object đầy đủ để match backend expect
        final cloudData = response.data;
        return {
          'errCode': 0,
          'data': [
            {
              'secure_url': cloudData['secure_url'],
              'public_id': cloudData['public_id'],
              'original_filename': cloudData['original_filename'],
              'format': cloudData['format'],
              'created_at': cloudData['created_at'],
            },
          ],
        };
      } else {
        print(
          'Cloudinary upload error: ${response.statusCode} - ${response.data}',
        );
        return {
          'errCode': 1,
          'errMessage':
              'Backend error: ${response.statusCode} - ${response.data}',
        };
      }
    } catch (e) {
      print('Upload error: $e');
      return {'errCode': 1, 'errMessage': e.toString()};
    }
  }

  Future<void> _pickImage() async {
    final permission = await Permission.photos.request();
    if (permission.isDenied) {
      Fluttertoast.showToast(msg: 'Vui lòng cấp quyền truy cập ảnh');
      return;
    }
    final picker = ImagePicker();
    final pickedFile = await picker.pickImage(source: ImageSource.gallery);
    if (pickedFile != null) {
      setState(() => _imageFile = File(pickedFile.path));
    }
  }

  void _viewInvoices() {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => ViewInvoiceScreen()),
    );
  }

  void _handleLogout() async {
    final userProvider = Provider.of<UserProvider>(context, listen: false);
    await userProvider.logoutUser();
    Navigator.pushReplacementNamed(context, '/login');
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_userInfo == null) {
      return const Scaffold(body: Center(child: Text('Lỗi tải thông tin')));
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thông tin người dùng'),
        backgroundColor: Colors.white,
      ),

      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            GestureDetector(
              onTap: _pickImage,
              child: CircleAvatar(
                radius: 50,
                backgroundImage: _imageFile != null
                    ? FileImage(_imageFile!)
                    : NetworkImage(_userImage) as ImageProvider,
              ),
            ),
            const SizedBox(height: 10),
            _buildField('Email', _userInfo!.email),
            _buildField('Tên tài khoản', _userInfo!.accountName, 'accountname'),
            _buildField('Họ và tên', _userInfo!.userName, 'username'),
            _buildField('Số điện thoại', _userInfo!.phone, 'phone'),
            _buildField('Địa chỉ', _userInfo!.address, 'address'),
            _buildGenderField(),
            const SizedBox(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                ElevatedButton(
                  onPressed: _viewInvoices,
                  child: const Row(
                    children: [
                      Text('Hóa đơn'),
                      SizedBox(width: 8),
                      Icon(Ionicons.receipt_outline),
                    ],
                  ),
                ),
                ElevatedButton(
                  onPressed: _handleLogout,
                  child: const Row(
                    children: [
                      Text('Đăng Xuất'),
                      SizedBox(width: 8),
                      Icon(Ionicons.log_out_outline),
                    ],
                  ),
                ),
              ],
            ),
            if (_editField.isNotEmpty || _imageFile != null) ...[
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: [
                  ElevatedButton(
                    onPressed: _cancelEdit,
                    child: const Text('Hủy'),
                  ),
                  ElevatedButton(
                    onPressed: _saveChanges,
                    child: const Text('Lưu'),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildField(String label, String value, [String? field]) {
    return ListTile(
      title: Text(label),
      subtitle: _editField == field
          ? TextField(onChanged: (val) => _tempValue = val)
          : Text(value),
      trailing: field != null
          ? IconButton(
              icon: const Icon(Icons.edit),
              onPressed: () => _editClick(field),
            )
          : null,
    );
  }

  Widget _buildGenderField() {
    return ListTile(
      title: const Text('Giới tính'),
      subtitle: _editField == 'gender'
          ? DropdownButton<String>(
              value: _tempValue,
              items: _genders
                  .map(
                    (g) => DropdownMenuItem<String>(
                      value: g['Code'],
                      child: Text(g['CodeValueVI']),
                    ),
                  )
                  .toList(),
              onChanged: (val) => setState(() => _tempValue = val!),
            )
          : Text(
              _genders.firstWhere(
                (g) => g['Code'] == _userInfo!.gender,
              )['CodeValueVI'],
            ),
      trailing: IconButton(
        icon: const Icon(Icons.edit),
        onPressed: () => _editClick('gender'),
      ),
    );
  }
}
