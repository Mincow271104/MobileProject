import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../utils/api.dart';
import '../providers/user_provider.dart';
import '../models/cart_model.dart';

class CartProvider with ChangeNotifier {
  int _cartItemCount = 0;
  List<CartItemModel> _cartItems = [];

  int get cartItemCount => _cartItemCount;
  List<CartItemModel> get cartItems => _cartItems;

  Future<void> loadCartCount(BuildContext context) async {
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final accountId = user?.accountId ?? '';

    if (accountId.isEmpty) {
      _cartItemCount = 0;
      notifyListeners();
      return;
    }

    final cartResponse = await apiService.getCart(accountId);
    if (cartResponse['errCode'] == 0 && cartResponse['data'] != null) {
      final List<dynamic> rawCartList = cartResponse['data'] as List<dynamic>;
      int totalQuantity = 0;
      for (var item in rawCartList) {
        totalQuantity += (item['ItemQuantity'] as int? ?? 0);
      }
      _cartItemCount = totalQuantity;
    } else {
      _cartItemCount = 0;
    }
    notifyListeners();
  }

  Future<void> loadCartItems(BuildContext context) async {
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final accountId = user?.accountId ?? '';

    if (accountId.isEmpty) {
      _cartItems = [];
      notifyListeners();
      return;
    }

    final cartResponse = await apiService.getCart(accountId);
    if (cartResponse['errCode'] == 0 && cartResponse['data'] != null) {
      final List<Map<String, dynamic>> cartInfo =
          (cartResponse['data'] as List<dynamic>)
              .map((item) => item as Map<String, dynamic>)
              .toList();
      final detailResponse = await apiService.getCartDetail(cartInfo);
      if (detailResponse['errCode'] == 0 && detailResponse['data'] != null) {
        _cartItems = (detailResponse['data'] as List<dynamic>)
            .map((json) => CartItemModel.fromJson(json as Map<String, dynamic>))
            .toList();
      } else {
        _cartItems = [];
      }
    } else {
      _cartItems = [];
    }
    notifyListeners();
  }

  void clearCartItems() {
    _cartItems = [];
    _cartItemCount = 0;
    notifyListeners();
  }
}
