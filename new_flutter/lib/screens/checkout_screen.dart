import 'package:flutter/material.dart';
import 'package:ionicons/ionicons.dart';
import 'package:provider/provider.dart';
import 'package:fluttertoast/fluttertoast.dart';
import '../utils/api.dart';
import '../providers/user_provider.dart';
import '../providers/cart_provider.dart';
import '../models/cart_model.dart';
import 'dashboard_screen.dart';
import 'invoice_screen.dart';

class CheckoutScreen extends StatefulWidget {
  final List<CartItemModel>? selectedItems; // Thêm param này
  const CheckoutScreen({super.key, this.selectedItems});

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  final _formKey = GlobalKey<FormState>();
  String _receiverName = '';
  String _receiverPhone = '';
  String _receiverAddress = '';
  String _receiverEmail = '';
  String _paymentType = '';
  String _shippingMethod = '';
  String _couponCode = '';
  String _tempCouponCode = '';
  double _discountAmount = 0.0;
  double _shippingFee = 0.0;
  List<Map<String, dynamic>> _paymentTypes = [];
  List<Map<String, dynamic>> _shippingMethods = [];
  bool _isLoading = true;
  bool _isApplyingCoupon = false;
  bool _isCardPayment = false;
  String _cardNumber = '';
  String _cardHolderName = '';
  String _expiryDate = '';
  String _cvv = '';
  bool _isBuyNow =
      false; // Tạm false, nếu từ buy now thì truyền param và set true

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

    // Pre-fill user info if logged in
    if (user != null) {
      final accountResponse = await apiService.getAccountInfo(user.accountId);
      if (accountResponse['errCode'] == 0 && accountResponse['data'] != null) {
        final accountData = accountResponse['data'];
        _receiverName = accountData['UserName'] ?? '';
        _receiverPhone = accountData['Phone'] ?? '';
        _receiverAddress = accountData['Address'] ?? '';
        _receiverEmail = accountData['Email'] ?? '';
      }
    }

    // Load Payment Types
    final paymentResponse = await apiService.getAllCodes('PaymentType');
    if (paymentResponse['errCode'] == 0 && paymentResponse['data'] != null) {
      _paymentTypes = List<Map<String, dynamic>>.from(paymentResponse['data']);
      _paymentType = _paymentTypes.isNotEmpty ? _paymentTypes[0]['Code'] : '';
      _isCardPayment = _paymentType == 'CARD';
    }

    // Load Shipping Methods
    final shippingResponse = await apiService.getAllCodes('ShippingMethod');
    if (shippingResponse['errCode'] == 0 && shippingResponse['data'] != null) {
      _shippingMethods = List<Map<String, dynamic>>.from(
        shippingResponse['data'],
      );
      _shippingMethod = _shippingMethods.isNotEmpty
          ? _shippingMethods[0]['Code']
          : '';
      _shippingFee = _shippingMethods.isNotEmpty
          ? double.tryParse(_shippingMethods[0]['ExtraValue'].toString()) ?? 0.0
          : 0.0;
    }

    setState(() => _isLoading = false);
  }

  List<CartItemModel> _getEffectiveCartItems() {
    final cartProvider = Provider.of<CartProvider>(context, listen: false);
    return widget.selectedItems ?? cartProvider.cartItems;
  }

  double _calculateTotalPrice(List<CartItemModel> items) {
    return items.fold(
      0.0,
      (sum, item) => sum + (item.itemPrice * item.itemQuantity),
    );
  }

  int _calculateTotalQuantity(List<CartItemModel> items) {
    return items.fold(0, (sum, item) => sum + item.itemQuantity);
  }

  double _calculateTotalPriceAfterPromo(List<CartItemModel> items) {
    return items.fold(0.0, (sum, item) {
      double price = item.itemPrice;
      if (item.promotion > 0) {
        price *= (1 - (item.promotion / 100));
      }
      return sum + (price * item.itemQuantity);
    });
  }

  double _calculateTotalPayment(double totalAfterPromo) {
    return totalAfterPromo + _shippingFee - _discountAmount;
  }

  Future<void> _applyCoupon() async {
    if (_tempCouponCode.isEmpty) return;
    setState(() => _isApplyingCoupon = true);

    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final totalAfterPromo =
        _calculateTotalPriceAfterPromo(_getEffectiveCartItems()) + _shippingFee;

    // Get coupon info
    final couponResponse = await apiService.getCouponInfo(_tempCouponCode);
    if (couponResponse['errCode'] != 0 || couponResponse['data'] == null) {
      Fluttertoast.showToast(
        msg: couponResponse['errMessage'] ?? 'Coupon không hợp lệ!',
        backgroundColor: Colors.red,
      );
      setState(() {
        _discountAmount = 0.0;
        _couponCode = '';
        _isApplyingCoupon = false;
      });
      return;
    }

    final couponData = couponResponse['data'];
    if (totalAfterPromo <
            (double.tryParse(couponData['MinOrderValue'].toString()) ?? 0.0) ||
        couponData['CouponStatus'] != 'ACTIVE') {
      Fluttertoast.showToast(
        msg: 'Không đủ điều kiện áp dụng coupon!',
        backgroundColor: Colors.red,
      );
      setState(() {
        _discountAmount = 0.0;
        _couponCode = '';
        _isApplyingCoupon = false;
      });
      return;
    }

    // Check coupon discount
    final checkResponse = await apiService.checkCoupon(
      _tempCouponCode,
      totalAfterPromo,
    );
    if (checkResponse['errCode'] == 0 && checkResponse['data'] != null) {
      _discountAmount =
          double.tryParse(checkResponse['data'].toString()) ?? 0.0;
      _couponCode = _tempCouponCode;
      Fluttertoast.showToast(msg: 'Áp dụng coupon thành công!');
    } else {
      _discountAmount = 0.0;
      _couponCode = '';
      Fluttertoast.showToast(
        msg: checkResponse['errMessage'] ?? 'Lỗi áp dụng coupon!',
        backgroundColor: Colors.red,
      );
    }

    setState(() => _isApplyingCoupon = false);
  }

  Future<void> _placeOrder() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();

    final cartProvider = Provider.of<CartProvider>(context, listen: false);
    final userProvider = Provider.of<UserProvider>(context, listen: false);
    final apiService = userProvider.apiService;
    final user = userProvider.user;

    Map<String, dynamic>? cardInfo;
    if (_paymentType == 'CARD') {
      if (_cardNumber.isEmpty ||
          _cardHolderName.isEmpty ||
          _expiryDate.isEmpty ||
          _cvv.isEmpty) {
        Fluttertoast.showToast(msg: 'Vui lòng nhập đầy đủ thông tin thẻ!');
        return;
      }
      cardInfo = {
        'cardNumber': _cardNumber,
        'cardholderName': _cardHolderName,
        'expiryDate': _expiryDate,
        'cvv': _cvv,
      };
    }

    String paymentStatus = _paymentType == 'CARD' ? 'PAID' : 'PEND';
    String shippingStatus = 'PEND';

    final effectiveItems = _getEffectiveCartItems();
    final cartItems = effectiveItems
        .map(
          (item) => {
            'productid': item.productId,
            'productdetailid': item.productDetailId,
            'itemprice': item.itemPrice,
            'itemquantity': item.itemQuantity,
          },
        )
        .toList();

    int? couponId;
    if (_couponCode.isNotEmpty) {
      final couponResponse = await apiService.getCouponInfo(_couponCode);
      if (couponResponse['errCode'] == 0 && couponResponse['data'] != null) {
        couponId = couponResponse['data']['CouponID'];
      }
    }

    final body = {
      'accountid': user?.accountId ?? null,
      'receivername': _receiverName,
      'receiverphone': _receiverPhone,
      'receiveraddress': _receiverAddress,
      'email': _receiverEmail.isNotEmpty ? _receiverEmail : null,
      'cartItems': cartItems,
      'totalquantity': _calculateTotalQuantity(effectiveItems),
      'totalprice': _calculateTotalPriceAfterPromo(effectiveItems),
      'discountamount': _discountAmount,
      'totalpayment': _calculateTotalPayment(
        _calculateTotalPriceAfterPromo(effectiveItems),
      ),
      'paymentstatus': paymentStatus,
      'shippingstatus': shippingStatus,
      'paymenttype': _paymentType,
      'shippingmethod': _shippingMethod,
      'couponid': couponId,
      'cartinfo': cardInfo,
      'isBuyNow': _isBuyNow,
    };

    setState(() => _isLoading = true);
    final response = await apiService.createInvoice(body);
    setState(() => _isLoading = false);

    if (response['errCode'] == 0 && response['data'] != null) {
      Fluttertoast.showToast(msg: 'Đặt hàng thành công!');
      cartProvider.clearCartItems();
      cartProvider.loadCartCount(context);
      final invoiceId =
          response['data']['InvoiceID']; // Giả sử response có 'InvoiceID', dựa trên BE
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (context) => InvoiceScreen(invoiceId: invoiceId),
        ),
      );
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Lỗi đặt hàng!',
        backgroundColor: Colors.red,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final cartProvider = Provider.of<CartProvider>(context);
    final effectiveItems = _getEffectiveCartItems();
    final totalPrice = _calculateTotalPrice(effectiveItems);
    final totalAfterPromo = _calculateTotalPriceAfterPromo(effectiveItems);
    final totalPayment = _calculateTotalPayment(totalAfterPromo);

    if (_isLoading) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(color: Color(0xFF4CAF50)),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thanh Toán'),
        backgroundColor: const Color.fromARGB(255, 255, 255, 255),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Tóm tắt giỏ hàng',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 10),
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: effectiveItems.length,
                itemBuilder: (context, index) {
                  final item = effectiveItems[index];
                  return ListTile(
                    leading: Image.network(
                      item.productImage,
                      width: 50,
                      height: 50,
                      fit: BoxFit.cover,
                    ),
                    title: Text(item.productName),
                    subtitle: Text(
                      '${item.detailName} - SL: ${item.itemQuantity} - KM: ${item.promotion}%',
                    ),
                    trailing: Text(
                      '${(item.itemPrice * item.itemQuantity).toStringAsFixed(0)} đ',
                    ),
                  );
                },
              ),
              const SizedBox(height: 20),
              Text(
                'Tổng tiền hàng (trước KM): ${totalPrice.toStringAsFixed(0)} đ',
              ),
              Text('Sau KM: ${totalAfterPromo.toStringAsFixed(0)} đ'),
              const SizedBox(height: 20),

              const Text(
                'Thông tin khách hàng',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              TextFormField(
                initialValue: _receiverName,
                decoration: const InputDecoration(labelText: 'Họ tên'),
                validator: (value) =>
                    value!.isEmpty ? 'Vui lòng nhập họ tên' : null,
                onSaved: (value) => _receiverName = value!,
              ),
              TextFormField(
                initialValue: _receiverPhone,
                decoration: const InputDecoration(labelText: 'Số điện thoại'),
                validator: (value) =>
                    value!.isEmpty ? 'Vui lòng nhập số điện thoại' : null,
                onSaved: (value) => _receiverPhone = value!,
              ),
              TextFormField(
                initialValue: _receiverAddress,
                decoration: const InputDecoration(labelText: 'Địa chỉ'),
                validator: (value) =>
                    value!.isEmpty ? 'Vui lòng nhập địa chỉ' : null,
                onSaved: (value) => _receiverAddress = value!,
              ),
              TextFormField(
                initialValue: _receiverEmail,
                decoration: const InputDecoration(
                  labelText: 'Email (nếu cần gửi hóa đơn)',
                ),
                onSaved: (value) => _receiverEmail = value ?? '',
              ),
              const SizedBox(height: 20),

              const Text(
                'Phương thức giao hàng',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              ..._shippingMethods.map(
                (method) => RadioListTile<String>(
                  title: Text(
                    method['CodeValueVI'] + ' (${method['ExtraValue']} đ)',
                  ),
                  value: method['Code'],
                  groupValue: _shippingMethod,
                  onChanged: (value) {
                    setState(() {
                      _shippingMethod = value!;
                      _shippingFee =
                          double.tryParse(method['ExtraValue'].toString()) ??
                          0.0;
                    });
                  },
                ),
              ),
              const SizedBox(height: 20),

              const Text(
                'Phương thức thanh toán',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              ..._paymentTypes.map(
                (type) => RadioListTile<String>(
                  title: Text(type['CodeValueVI']),
                  value: type['Code'],
                  groupValue: _paymentType,
                  onChanged: (value) {
                    setState(() {
                      _paymentType = value!;
                      _isCardPayment = value == 'CARD';
                    });
                  },
                ),
              ),
              if (_isCardPayment) ...[
                const Text(
                  'Nhập thông tin thẻ',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
                TextFormField(
                  decoration: const InputDecoration(
                    labelText: 'Số thẻ (13-16 chữ số)',
                  ),
                  onChanged: (value) => _cardNumber = value,
                  validator: (value) =>
                      value!.isEmpty ? 'Vui lòng nhập số thẻ' : null,
                ),
                TextFormField(
                  decoration: const InputDecoration(labelText: 'Tên trên thẻ'),
                  onChanged: (value) => _cardHolderName = value,
                  validator: (value) =>
                      value!.isEmpty ? 'Vui lòng nhập tên chủ thẻ' : null,
                ),
                TextFormField(
                  decoration: const InputDecoration(labelText: 'MM/YY'),
                  onChanged: (value) => _expiryDate = value,
                  validator: (value) =>
                      value!.isEmpty ? 'Vui lòng nhập ngày hết hạn' : null,
                ),
                TextFormField(
                  decoration: const InputDecoration(
                    labelText: 'CVV (3 chữ số)',
                  ),
                  onChanged: (value) => _cvv = value,
                  validator: (value) =>
                      value!.isEmpty ? 'Vui lòng nhập CVV' : null,
                ),
              ],
              const SizedBox(height: 20),

              const Text(
                'Mã giảm giá',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      onChanged: (value) => _tempCouponCode = value,
                      decoration: const InputDecoration(labelText: 'Nhập mã'),
                    ),
                  ),
                  ElevatedButton(
                    onPressed: _isApplyingCoupon ? null : _applyCoupon,
                    child: _isApplyingCoupon
                        ? const CircularProgressIndicator()
                        : const Text(
                            'Áp dụng',
                            style: TextStyle(
                              color: Color.fromARGB(255, 0, 0, 0),
                            ),
                          ),
                  ),
                ],
              ),
              const SizedBox(height: 20),

              Text(
                'Tổng thanh toán: ${totalPayment.toStringAsFixed(0)} đ',
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: Colors.red,
                ),
              ),
              const SizedBox(height: 20),

              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _placeOrder,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color.fromARGB(255, 38, 38, 38),
                    minimumSize: const Size(double.infinity, 50),
                  ),
                  child: const Text(
                    'Hoàn tất thanh toán',
                    style: TextStyle(color: Colors.white, fontSize: 18),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
