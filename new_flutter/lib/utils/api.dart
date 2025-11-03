import 'dart:convert';
import 'dart:io'; // For File
import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:fluttertoast/fluttertoast.dart';
import '../models/user_model.dart';

const String baseUrl = 'http://10.0.2.2:9999/api';

class ApiService {
  final Dio _dio = Dio();
  final SharedPreferences _prefs;

  ApiService(this._prefs) {
    _dio.options.baseUrl = baseUrl;
    _dio.options.connectTimeout = const Duration(seconds: 10);
    _dio.options.receiveTimeout = const Duration(seconds: 10);

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = _prefs.getString('token');
          if (token != null) {
            options.headers['Cookie'] = 'token=$token';
          }
          print('Request: ${options.method} ${options.uri}');
          return handler.next(options);
        },
        onError: (DioException e, handler) {
          print('API Error: ${e.message}');
          if (e.response?.statusCode == 401) {
            Fluttertoast.showToast(
              msg: 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.',
            );
          } else {
            Fluttertoast.showToast(
              msg: 'Lỗi kết nối: ${e.message ?? 'Kiểm tra server'}',
            );
          }
          return handler.resolve(
            Response(
              requestOptions: e.requestOptions,
              data: {
                'errCode': 3,
                'errMessage': e.message ?? 'Lỗi không xác định',
              },
            ),
          );
        },
      ),
    );
  }

  static Future<ApiService> create() async {
    final prefs = await SharedPreferences.getInstance();
    return ApiService(prefs);
  }

  // ==================== AUTH ====================

  Future<Map<String, dynamic>> login({
    required String accountName,
    required String password,
    bool rememberMe = false,
  }) async {
    try {
      final response = await _dio.post(
        '/login',
        data: {
          'accountname': accountName,
          'password': password,
          'rememberLogin': rememberMe,
        },
        options: Options(
          followRedirects: false,
          validateStatus: (status) => status! < 500,
        ),
      );

      print('Login Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        final token = response.data['token'];
        final user = UserModel.fromJson(response.data['data']);
        if (rememberMe) {
          await _prefs.setString('token', token);
        }
        return {'success': true, 'user': user, 'token': token};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Đăng nhập thất bại',
        );
        return {'success': false};
      }
    } on DioException catch (e) {
      print('Login Error: ${e.response?.data ?? e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message}');
      return {'success': false};
    }
  }

  Future<Map<String, dynamic>> verifyToken() async {
    final token = _prefs.getString('token');
    if (token == null) return {'success': false};

    try {
      final response = await _dio.get(
        '/verify-token',
        options: Options(headers: {'Cookie': 'token=$token'}),
      );

      if (response.data['errCode'] == 0) {
        final user = UserModel.fromJson(response.data['data']);
        return {'success': true, 'user': user};
      } else {
        await _prefs.remove('token');
        return {'success': false};
      }
    } on DioException catch (e) {
      print('Verify Token Error: ${e.message}');
      await _prefs.remove('token');
      return {'success': false};
    }
  }

  Future<void> logout() async {
    try {
      final token = _prefs.getString('token');
      if (token != null) {
        await _dio.get(
          '/logout',
          options: Options(headers: {'Cookie': 'token=$token'}),
        );
        await _prefs.remove('token');
      }
      Fluttertoast.showToast(msg: 'Đăng xuất thành công');
    } catch (e) {
      print('Logout Error: $e');
      Fluttertoast.showToast(msg: 'Lỗi đăng xuất');
    }
  }

  Future<Map<String, dynamic>> register({
    required String accountName,
    required String email,
    required String password,
    required String userName,
    required String phone,
    required String address,
    required String gender,
    String accountType = 'C',
  }) async {
    try {
      final response = await _dio.post(
        '/register',
        data: {
          'accountname': accountName,
          'email': email,
          'password': password,
          'username': userName,
          'phone': phone,
          'address': address,
          'gender': gender,
          'accounttype': accountType,
        },
      );

      print('Register Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        Fluttertoast.showToast(msg: 'Đăng ký thành công!');
        return {'success': true, 'data': response.data['data']};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Đăng ký thất bại',
        );
        return {'success': false};
      }
    } on DioException catch (e) {
      print('Register Error: ${e.response?.data ?? e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message}');
      return {'success': false};
    }
  }

  // ==================== PRODUCT ====================

  Future<Map<String, dynamic>> getProductInfo(String productId) async {
    try {
      final response = await _dio.get(
        '/get-productinfo',
        queryParameters: {'productid': productId},
      );

      print('GetProductInfo Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true, 'data': response.data['data']};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Lỗi tải sản phẩm',
        );
        return {'success': false};
      }
    } on DioException catch (e) {
      print('GetProductInfo Error: ${e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message}');
      return {'success': false};
    }
  }

  Future<Map<String, dynamic>> getSaleBannerInfo() async {
    try {
      final response = await _dio.get(
        '/get-sale-bannerinfo',
        queryParameters: {'productid': 'ALL'},
      );

      print('GetSaleBannerInfo Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true, 'data': response.data['data']};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Lỗi tải banner',
        );
        return {'success': false};
      }
    } on DioException catch (e) {
      print('GetSaleBannerInfo Error: ${e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message}');
      return {'success': false};
    }
  }

  Future<Map<String, dynamic>> loadSaleProductInfo({
    int page = 1,
    int limit = 10,
    String search = '',
    String filter = 'ALL',
    String sort = '0',
  }) async {
    try {
      final response = await _dio.get(
        '/load-sale-productinfo',
        queryParameters: {
          'page': page,
          'limit': limit,
          'search': search.isNotEmpty ? search : null,
          'filter': filter != 'ALL' ? filter : null,
          'sort': sort != '0' ? sort : null,
        }..removeWhere((key, value) => value == null),
      );

      print('LoadSaleProductInfo Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {
          'success': true,
          'data': response.data['data'],
          'totalItems': response.data['totalItems'],
        };
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Lỗi tải sản phẩm',
        );
        return {'success': false};
      }
    } on DioException catch (e) {
      print('LoadSaleProductInfo Error: ${e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message}');
      return {'success': false};
    }
  }

  Future<Map<String, dynamic>> getProductDetailInfo(
    String productId,
    int? productDetailId,
  ) async {
    try {
      final response = await _dio.get(
        '/get-productdetailinfo',
        queryParameters: {
          'productid': productId,
          if (productDetailId != null) 'productdetailid': productDetailId,
        },
      );

      print('GetProductDetailInfo Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true, 'data': response.data['data']};
      } else {
        return {'success': false, 'errMessage': response.data['errMessage']};
      }
    } on DioException catch (e) {
      print('GetProductDetailInfo Error: ${e.message}');
      return {'success': false, 'errMessage': e.message};
    }
  }

  Future<Map<String, dynamic>> getSaleProductInfo(String productId) async {
    try {
      final response = await _dio.get(
        '/get-sale-productinfo',
        queryParameters: {'productid': productId},
      );

      print('GetSaleProductInfo Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true, 'data': response.data['data']};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Lỗi tải chi tiết',
        );
        return {'success': false};
      }
    } on DioException catch (e) {
      print('GetSaleProductInfo Error: ${e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message}');
      return {'success': false};
    }
  }

  // ==================== CART ====================

  Future<Map<String, dynamic>> getCart(String accountId) async {
    try {
      final response = await _dio.get(
        '/get-cart',
        queryParameters: {'accountid': accountId},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> getCartDetail(
    List<Map<String, dynamic>> cartInfo,
  ) async {
    try {
      final response = await _dio.get(
        '/get-cartdetail',
        queryParameters: {'cartInfo': jsonEncode(cartInfo)},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> getDetailList(
    List<Map<String, dynamic>> cartInfo,
  ) async {
    try {
      final response = await _dio.get(
        '/get-detaillist',
        queryParameters: {'cartInfo': jsonEncode(cartInfo)},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> addToCart(
    String accountId,
    List<Map<String, dynamic>> cartInfo,
  ) async {
    try {
      final response = await _dio.post(
        '/add-to-cart',
        data: {'accountid': accountId, 'cartInfo': cartInfo},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> updateQuantity(
    String accountId,
    String productId,
    int productDetailId,
    int quantity,
  ) async {
    try {
      final response = await _dio.put(
        '/update-quantity',
        data: {
          'accountid': accountId,
          'productid': productId,
          'productdetailid': productDetailId,
          'quantity': quantity,
        },
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> updateCartDetail(
    String accountId,
    String productId,
    int productDetailId1,
    int productDetailId2,
  ) async {
    try {
      final response = await _dio.put(
        '/update-cart-detail',
        data: {
          'accountid': accountId,
          'productid': productId,
          'productdetailid1': productDetailId1,
          'productdetailid2': productDetailId2,
        },
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> mergeCartDetail(
    String accountId,
    String productId,
    int productDetailId1,
    int productDetailId2,
    int quantity,
  ) async {
    try {
      final response = await _dio.put(
        '/merge-cart-detail',
        data: {
          'accountid': accountId,
          'productid': productId,
          'productdetailid1': productDetailId1,
          'productdetailid2': productDetailId2,
          'quantity': quantity,
        },
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> removeFromCart(
    String accountId,
    String productId,
    int productDetailId,
  ) async {
    try {
      final response = await _dio.delete(
        '/remove-from-cart',
        data: {
          'accountid': accountId,
          'productid': productId,
          'productdetailid': productDetailId,
        },
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  // ==================== UTILITIES ====================

  Future<Map<String, dynamic>> getAllCodes(String type) async {
    try {
      final response = await _dio.get(
        '/get-allcodes',
        queryParameters: {'type': type},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  // ==================== COUPON ====================

  Future<Map<String, dynamic>> getCouponInfo(String couponcode) async {
    try {
      final response = await _dio.get(
        '/get-couponinfo',
        queryParameters: {'couponcode': couponcode},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> checkCoupon(
    String couponcode,
    double price,
  ) async {
    try {
      final response = await _dio.get(
        '/check-coupon',
        queryParameters: {'couponcode': couponcode, 'price': price},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  // ==================== INVOICE ====================

  Future<Map<String, dynamic>> createInvoice(Map<String, dynamic> body) async {
    try {
      final response = await _dio.post('/create-invoice', data: body);
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> getInvoiceDetailInfo(String invoiceId) async {
    try {
      final response = await _dio.get(
        '/get-invoicedetailinfo',
        queryParameters: {'invoiceid': invoiceId},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> sendInvoiceEmail(
    String billid,
    String email,
  ) async {
    try {
      final response = await _dio.post(
        '/get-invoice-email',
        data: {'billid': billid, 'email': email},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> changeInvoiceStatus(
    String invoiceId,
    String type,
    String status,
    String cancelReason,
  ) async {
    try {
      final response = await _dio.put(
        '/change-invoicestatus',
        data: {
          'invoiceid': invoiceId,
          'type': type,
          'status': status,
          'cancelReason': cancelReason,
        },
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> getAccountInvoiceInfo(String accountId) async {
    try {
      final response = await _dio.get(
        '/get-account-invoiceinfo',
        queryParameters: {'accountid': accountId},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  // ==================== ACCOUNT ====================

  Future<Map<String, dynamic>> getAccountInfo(String accountId) async {
    try {
      final response = await _dio.get(
        '/get-accountinfo',
        queryParameters: {'accountid': accountId},
      );
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> changeAccountInfo(
    Map<String, dynamic> body,
  ) async {
    try {
      final response = await _dio.put('/change-accountinfo', data: body);
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  Future<Map<String, dynamic>> uploadImageToCloudinary(File file) async {
    try {
      FormData formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(file.path),
      });
      final response = await _dio.post('/upload-image', data: formData);
      return response.data;
    } catch (e) {
      return {'errCode': 3, 'errMessage': e.toString()};
    }
  }

  // ==================== FORGOT PASSWORD ====================

  Future<Map<String, dynamic>> sendForgotToken(String email) async {
    try {
      final response = await _dio.post(
        '/send-forgot-token',
        data: {'email': email},
      );

      print('SendForgotToken Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true, 'accountID': response.data['data']};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Lỗi kiểm tra email',
        );
        return {'success': false, 'message': response.data['errMessage']};
      }
    } on DioException catch (e) {
      print('SendForgotToken Error: ${e.response?.data ?? e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message ?? 'Kết nối thất bại'}');
      return {'success': false, 'message': e.message ?? 'Lỗi kết nối'};
    }
  }

  Future<Map<String, dynamic>> verifyForgotToken(
    String accountID,
    String verificationCode,
  ) async {
    try {
      final response = await _dio.post(
        '/verify-forgot-token',
        data: {'accountid': accountID, 'token': verificationCode},
      );

      print('VerifyForgotToken Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Mã xác nhận không hợp lệ',
        );
        return {'success': false, 'message': response.data['errMessage']};
      }
    } on DioException catch (e) {
      print('VerifyForgotToken Error: ${e.response?.data ?? e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message ?? 'Kết nối thất bại'}');
      return {'success': false, 'message': e.message ?? 'Lỗi kết nối'};
    }
  }

  Future<Map<String, dynamic>> changeForgotPassword(
    String accountID,
    String newPassword,
  ) async {
    try {
      final response = await _dio.put(
        '/change-password',
        data: {
          'accountid': accountID,
          'password': 'forgot_password',
          'newpassword': newPassword,
        },
      );

      print('ChangeForgotPassword Response: ${response.data}');

      if (response.data['errCode'] == 0) {
        return {'success': true};
      } else {
        Fluttertoast.showToast(
          msg: response.data['errMessage'] ?? 'Lỗi đổi mật khẩu',
        );
        return {'success': false, 'message': response.data['errMessage']};
      }
    } on DioException catch (e) {
      print('ChangeForgotPassword Error: ${e.response?.data ?? e.message}');
      Fluttertoast.showToast(msg: 'Lỗi: ${e.message ?? 'Kết nối thất bại'}');
      return {'success': false, 'message': e.message ?? 'Lỗi kết nối'};
    }
  }
}
