import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:ionicons/ionicons.dart';
import 'package:fluttertoast/fluttertoast.dart';
import '../utils/api.dart';
import '../providers/user_provider.dart';
import 'package:ionicons/ionicons.dart';
import '../providers/cart_provider.dart';
import '../models/cart_model.dart';
import '../models/product_type_model.dart';
import 'dashboard_screen.dart';
import 'checkout_screen.dart'; // Thêm import này

class CartScreen extends StatefulWidget {
  const CartScreen({super.key});

  @override
  State<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends State<CartScreen> {
  CartItemModel? _selectedItem;
  ProductTypeModel? _selectedType;
  int _quantity = 1;
  List<ProductTypeModel> _productTypes = [];
  bool _isLoading = true;
  Set<int> _selectedIndices = {};

  @override
  void initState() {
    super.initState();
    _loadCart();
  }

  Future<void> _loadCart() async {
    setState(() => _isLoading = true);
    final cartProvider = Provider.of<CartProvider>(context, listen: false);
    await cartProvider.loadCartItems(context);
    String? oldProductId;
    int? oldDetailId;
    if (_selectedItem != null) {
      oldProductId = _selectedItem!.productId;
      oldDetailId = _selectedItem!.productDetailId;
    }
    setState(() {
      _isLoading = false;
      _selectedIndices.clear();
      _selectedItem = null;
      _selectedType = null;
      _productTypes = [];
      _quantity = 1;
    });
    // Re-set _selectedItem nếu tồn tại
    if (oldProductId != null && oldDetailId != null) {
      final newSelected = cartProvider.cartItems.firstWhere(
        (item) =>
            item.productId == oldProductId &&
            item.productDetailId == oldDetailId,
        orElse: () => CartItemModel(
          cartItemId: 0,
          productId: '',
          productDetailId: 0,
          itemPrice: 0.0,
          itemQuantity: 0,
          productName: '',
          productImage: '',
          detailName: '',
          stock: 0,
          promotion: 0.0,
        ),
      );
      if (newSelected.productId.isNotEmpty) {
        _selectItem(newSelected);
      }
    }
  }

  void _selectItem(CartItemModel item) async {
    setState(() {
      _selectedItem = item;
      _quantity = item.itemQuantity;
    });

    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final List<Map<String, dynamic>> cartInfo = [
      {'ProductID': item.productId},
    ];

    final listResponse = await apiService.getDetailList(cartInfo);

    if (listResponse['errCode'] == 0 && listResponse['data'] != null) {
      final rawList = listResponse['data'] as List;
      final detailData = rawList.isNotEmpty ? rawList[0] : null;

      if (detailData != null && detailData['DetailList'] != null) {
        final List<dynamic> rawDetailList = detailData['DetailList'];
        setState(() {
          _productTypes = rawDetailList
              .map(
                (json) =>
                    ProductTypeModel.fromJson(json as Map<String, dynamic>),
              )
              .toList();
          if (_productTypes.isNotEmpty) {
            _selectedType = _productTypes.firstWhere(
              (t) => t.productDetailId == item.productDetailId,
              orElse: () => _productTypes.first,
            );
          } else {
            _selectedType = null;
          }
        });
      } else {
        setState(() {
          _productTypes = [];
          _selectedType = null;
        });
      }
    } else {
      setState(() {
        _productTypes = [];
        _selectedType = null;
      });
    }
  }

  void _closeEditBar() {
    setState(() {
      _selectedItem = null;
      _selectedType = null;
      _quantity = 1;
      _productTypes = [];
    });
  }

  void _handleDelete(String productId, int productDetailId, int index) async {
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final accountId = user?.accountId ?? '';

    final response = await apiService.removeFromCart(
      accountId,
      productId,
      productDetailId,
    );
    if (response['errCode'] == 0) {
      Fluttertoast.showToast(msg: 'Xóa thành công');
      await _loadCart();
      Provider.of<CartProvider>(context, listen: false).loadCartCount(context);
      setState(() => _selectedItem = null);
    } else {
      Fluttertoast.showToast(msg: response['errMessage'] ?? 'Lỗi xóa');
    }
  }

  void _handleTypeChange(ProductTypeModel type) async {
    if (_selectedItem == null) {
      Fluttertoast.showToast(msg: 'Không có sản phẩm được chọn!');
      return;
    }

    final selectedProductId = _selectedItem!.productId;
    final selectedDetailId = _selectedItem!.productDetailId;

    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final accountId = user?.accountId ?? '';

    // Kiểm tra nếu loại mới trùng với loại đã có trong giỏ
    final cartProvider = Provider.of<CartProvider>(context, listen: false);
    CartItemModel existingItem = CartItemModel(
      cartItemId: 0,
      productId: '',
      productDetailId: 0,
      itemPrice: 0.0,
      itemQuantity: 0,
      productName: '',
      productImage: '',
      detailName: '',
      stock: 0,
      promotion: 0.0,
    );
    bool hasExisting = false;
    for (var i in cartProvider.cartItems) {
      if (i.productId == selectedProductId &&
          i.productDetailId == type.productDetailId) {
        existingItem = i;
        hasExisting = true;
        break;
      }
    }

    if (hasExisting && existingItem.productDetailId != selectedDetailId) {
      // Trùng loại → Gộp
      final newQuantity =
          _selectedItem!.itemQuantity + existingItem.itemQuantity;
      final response = await apiService.mergeCartDetail(
        accountId,
        selectedProductId,
        selectedDetailId,
        type.productDetailId,
        newQuantity,
      );

      if (response['errCode'] == 0) {
        Fluttertoast.showToast(msg: 'Gộp sản phẩm thành công');
        await _loadCart();
        Provider.of<CartProvider>(
          context,
          listen: false,
        ).loadCartCount(context);
      } else {
        Fluttertoast.showToast(
          msg: response['errMessage'] ?? 'Lỗi gộp sản phẩm',
        );
        return;
      }
    } else {
      // Không trùng → Đổi loại bình thường
      final response = await apiService.updateCartDetail(
        accountId,
        selectedProductId,
        selectedDetailId,
        type.productDetailId,
      );

      if (response['errCode'] != 0) {
        Fluttertoast.showToast(msg: response['errMessage'] ?? 'Lỗi đổi loại');
        return;
      }
    }

    // Reload và update
    await _loadCart();
    CartItemModel updatedItem = CartItemModel(
      cartItemId: 0,
      productId: '',
      productDetailId: 0,
      itemPrice: 0.0,
      itemQuantity: 0,
      productName: '',
      productImage: '',
      detailName: '',
      stock: 0,
      promotion: 0.0,
    );
    bool found = false;
    for (var i in cartProvider.cartItems) {
      if (i.productId == selectedProductId &&
          i.productDetailId == type.productDetailId) {
        updatedItem = i;
        found = true;
        break;
      }
    }
    if (found) {
      setState(() {
        _selectedItem = updatedItem;
        _selectedType = type;
        _quantity = updatedItem.itemQuantity;
      });
      Fluttertoast.showToast(msg: 'Đổi loại thành công');
    } else {
      setState(() {
        _selectedItem = null;
        _selectedType = null;
        _quantity = 1;
        _productTypes = [];
      });
      Fluttertoast.showToast(
        msg: 'Sản phẩm không còn tồn tại sau khi cập nhật',
      );
    }
  }

  void _handleQuantityChange(int delta) async {
    if (_selectedItem == null) {
      Fluttertoast.showToast(msg: 'Không có sản phẩm được chọn!');
      return;
    }

    final selectedProductId = _selectedItem!.productId;
    final selectedDetailId = _selectedItem!.productDetailId;

    final newQuantity = (_quantity + delta).clamp(1, _selectedType?.stock ?? 1);
    if (newQuantity == _quantity) return;

    setState(() => _quantity = newQuantity);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final accountId = user?.accountId ?? '';

    final response = await apiService.updateQuantity(
      accountId,
      selectedProductId,
      selectedDetailId,
      newQuantity,
    );

    if (response['errCode'] == 0) {
      Fluttertoast.showToast(msg: 'Cập nhật số lượng thành công');
      await _loadCart();
      Provider.of<CartProvider>(context, listen: false).loadCartCount(context);
      final cartProvider = Provider.of<CartProvider>(context, listen: false);
      CartItemModel updatedItem = CartItemModel(
        cartItemId: 0,
        productId: '',
        productDetailId: 0,
        itemPrice: 0.0,
        itemQuantity: 0,
        productName: '',
        productImage: '',
        detailName: '',
        stock: 0,
        promotion: 0.0,
      );
      bool found = false;
      for (var i in cartProvider.cartItems) {
        if (i.productId == selectedProductId &&
            i.productDetailId == selectedDetailId) {
          updatedItem = i;
          found = true;
          break;
        }
      }
      if (found) {
        setState(() {
          _selectedItem = updatedItem;
          _quantity = updatedItem.itemQuantity;
        });
      } else {
        setState(() {
          _selectedItem = null;
          _selectedType = null;
          _quantity = 1;
          _productTypes = [];
        });
        Fluttertoast.showToast(
          msg: 'Sản phẩm không còn tồn tại sau khi cập nhật',
        );
      }
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Lỗi cập nhật số lượng',
      );
      setState(() => _quantity = _selectedItem!.itemQuantity);
    }
  }

  double _getTotalCartPrice() {
    final cartProvider = Provider.of<CartProvider>(context, listen: false);
    if (_selectedIndices.isEmpty) {
      return cartProvider.cartItems.fold(
        0.0,
        (sum, item) => sum + (item.itemPrice * item.itemQuantity),
      );
    }
    return _selectedIndices.fold(0.0, (sum, index) {
      final item = cartProvider.cartItems[index];
      return sum + (item.itemPrice * item.itemQuantity);
    });
  }

  double _getFinalPrice() {
    if (_selectedItem == null || _selectedType == null) return 0;
    final unitPrice = _selectedItem!.itemPrice;
    return unitPrice * _quantity;
  }

  @override
  Widget build(BuildContext context) {
    final cartProvider = Provider.of<CartProvider>(context);
    return Scaffold(
      backgroundColor: const Color.fromRGBO(240, 227, 234, 1),
      appBar: AppBar(
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Ionicons.cart_outline, color: Color.fromRGBO(91, 85, 85, 1)),
            const SizedBox(width: 8),
            Text(
              'Giỏ hàng',
              style: TextStyle(
                color: Color.fromRGBO(91, 85, 85, 1),
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        backgroundColor: const Color.fromARGB(255, 255, 255, 255),
      ),
      body: _isLoading
          ? const Center(
              child: CircularProgressIndicator(color: Color(0xFF4CAF50)),
            )
          : cartProvider.cartItems.isEmpty
          ? const Center(child: Text('Giỏ hàng trống'))
          : ListView.builder(
              itemCount: cartProvider.cartItems.length,
              itemBuilder: (context, index) {
                if (index >= cartProvider.cartItems.length)
                  return const SizedBox();
                final item = cartProvider.cartItems[index];
                final isSelected = _selectedIndices.contains(index);

                // return ListTile(
                //   leading: Row(
                //     mainAxisSize: MainAxisSize.min,
                //     children: [
                //       SizedBox(
                //         width: 40,
                //         height: 40,
                //         child: Checkbox(
                //           value: isSelected,
                //           onChanged: (bool? value) {
                //             setState(() {
                //               if (value == true) {
                //                 _selectedIndices.add(index);
                //               } else {
                //                 _selectedIndices.remove(index);
                //               }
                //             });
                //           },
                //           shape: const CircleBorder(),
                //           side: const BorderSide(color: Colors.grey),
                //         ),
                //       ),
                //       ClipRRect(
                //         borderRadius: BorderRadius.circular(8),
                //         child: Image.network(
                //           item.productImage,
                //           width: 60,
                //           height: 60,
                //           fit: BoxFit.cover,
                //           errorBuilder: (_, __, ___) =>
                //               const Icon(Icons.image, size: 60),
                //         ),
                //       ),
                //     ],
                //   ),
                //   title: Text(
                //     '${item.productName} (${item.detailName})',
                //     style: const TextStyle(fontWeight: FontWeight.bold),
                //   ),
                //   trailing: IconButton(
                //     icon: const Icon(
                //       Ionicons.close_circle_outline,
                //       color: Colors.red,
                //     ),
                //     onPressed: () => _handleDelete(
                //       item.productId,
                //       item.productDetailId,
                //       index,
                //     ),
                //   ),
                //   onTap: () => _selectItem(item),
                // );
                return ListTile(
                  leading: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      SizedBox(
                        width: 40,
                        height: 40,
                        child: Checkbox(
                          value: isSelected,
                          onChanged: (bool? value) {
                            setState(() {
                              if (value == true) {
                                _selectedIndices.add(index);
                              } else {
                                _selectedIndices.remove(index);
                              }
                            });
                          },
                          shape: const CircleBorder(),
                          side: const BorderSide(color: Colors.grey),
                        ),
                      ),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: Image.network(
                          item.productImage,
                          width: 60,
                          height: 60,
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) =>
                              const Icon(Icons.image, size: 60),
                        ),
                      ),
                    ],
                  ),
                  title: Text(
                    '${item.productName} (${item.detailName})',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  subtitle: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('SL: ${item.itemQuantity}'),
                      Text(
                        'Tổng: ${(item.itemPrice * item.itemQuantity).toStringAsFixed(0)} đ',
                        style: const TextStyle(
                          color: Colors.red,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                  trailing: IconButton(
                    icon: const Icon(
                      Ionicons.close_circle_outline,
                      color: Colors.red,
                    ),
                    onPressed: () => _handleDelete(
                      item.productId,
                      item.productDetailId,
                      index,
                    ),
                  ),
                  onTap: () => _selectItem(item),
                );
              },
            ),
      bottomNavigationBar: _selectedItem != null
          ? _buildEditBar()
          : _buildTotalBar(),
    );
  }

  Widget _buildTotalBar() {
    final totalPrice = _getTotalCartPrice();
    final hasSelection = _selectedIndices.isNotEmpty;
    return Container(
      padding: const EdgeInsets.all(12.0),
      color: Colors.grey[100],
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                hasSelection
                    ? 'Tổng đã chọn: ${totalPrice.toStringAsFixed(0)} đ'
                    : 'Tổng tiền: ${totalPrice.toStringAsFixed(0)} đ',
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: Colors.red,
                ),
              ),
              ElevatedButton(
                onPressed: () {
                  final cartProvider = Provider.of<CartProvider>(
                    context,
                    listen: false,
                  );
                  List<CartItemModel>? selected;
                  if (_selectedIndices.isNotEmpty) {
                    selected = _selectedIndices
                        .map((i) => cartProvider.cartItems[i])
                        .toList();
                  }
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) =>
                          CheckoutScreen(selectedItems: selected),
                    ),
                  );
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color.fromARGB(255, 255, 255, 255),
                  side: BorderSide(
                    color: Color.fromRGBO(91, 85, 85, 1),
                    width: 2.0,
                  ),
                ),
                child: Text(
                  hasSelection ? 'Thanh toán đã chọn' : 'Thanh toán toàn bộ',
                  style: const TextStyle(color: Color.fromRGBO(91, 85, 85, 1)),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildEditBar() {
    return Container(
      padding: const EdgeInsets.all(12.0),
      color: Colors.grey[100],
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Expanded(
                flex: 4,
                child: DropdownButton<ProductTypeModel>(
                  isExpanded: true,
                  value: _selectedType,
                  hint: const Text('Chọn loại'),
                  items: _productTypes
                      .map(
                        (type) => DropdownMenuItem(
                          value: type,
                          child: Text(type.detailName),
                        ),
                      )
                      .toList(),
                  onChanged: (type) => _handleTypeChange(type!),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                width: 60,
                child: Text(
                  'Kho: ${_selectedType?.stock ?? 0}',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                  textAlign: TextAlign.center,
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                width: 60,
                child: Text(
                  'KM: ${_selectedType?.promotion ?? 0}%',
                  style: const TextStyle(
                    color: Colors.red,
                    fontWeight: FontWeight.bold,
                  ),
                  textAlign: TextAlign.center,
                ),
              ),
              IconButton(
                icon: const Icon(Ionicons.close, color: Colors.red),
                onPressed: _closeEditBar,
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SizedBox(
                    width: 32,
                    child: IconButton(
                      padding: EdgeInsets.zero,
                      onPressed: () => _handleQuantityChange(-1),
                      icon: const Icon(
                        Ionicons.remove_circle_outline,
                        size: 20,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: 40,
                    child: Text(
                      '$_quantity',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: 32,
                    child: IconButton(
                      padding: EdgeInsets.zero,
                      onPressed: () => _handleQuantityChange(1),
                      icon: const Icon(Ionicons.add_circle_outline, size: 20),
                    ),
                  ),
                ],
              ),
              Text(
                'Tổng: ${_getFinalPrice().toStringAsFixed(0)} đ',
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: Colors.red,
                ),
              ),
              Text('SL: $_quantity', style: const TextStyle(fontSize: 16)),
            ],
          ),
        ],
      ),
    );
  }
}
