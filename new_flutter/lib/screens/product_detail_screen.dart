import 'package:flutter/material.dart';
import 'package:ionicons/ionicons.dart';
import 'package:provider/provider.dart';
import '../utils/api.dart';
import '../providers/user_provider.dart';
import '../models/product_detail_model.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'dashboard_screen.dart'; // THÊM IMPORT (nếu cần, nhưng có thể không liên quan đến fix)
import '../providers/cart_provider.dart';

class ProductDetailScreen extends StatefulWidget {
  final String productId;
  const ProductDetailScreen({super.key, required this.productId});
  @override
  State<ProductDetailScreen> createState() => _ProductDetailScreenState();
}

class _ProductDetailScreenState extends State<ProductDetailScreen> {
  ProductDetailModel? _product;
  ProductTypeModel? _selectedType;
  int _quantity = 1;
  String _selectedImage = '';
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadProductDetails();
  }

  Future<void> _loadProductDetails() async {
    setState(() => _isLoading = true);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final response = await apiService.getSaleProductInfo(widget.productId);
    if (response['success'] && response['data'] != null) {
      setState(() {
        _product = ProductDetailModel.fromJson(response['data']);
        _selectedType = _product!.productTypes.isNotEmpty
            ? _product!.productTypes[0]
            : null;
        _selectedImage = _product!.productImage;
        _isLoading = false;
      });
    } else {
      Fluttertoast.showToast(msg: 'Không thể tải chi tiết sản phẩm');
      setState(() => _isLoading = false);
    }
  }

  void _handleTypeChange(ProductTypeModel type) {
    setState(() {
      _selectedType = type;
      _quantity = 1;
    });
  }

  void _handleQuantityIncrease() {
    if (_selectedType != null && _quantity < _selectedType!.stock) {
      setState(() => _quantity++);
    }
  }

  void _handleQuantityDecrease() {
    if (_quantity > 1) {
      setState(() => _quantity--);
    }
  }

  void _handleImageClick(String image) {
    setState(() => _selectedImage = image);
  }

  double _getBasePrice() {
    if (_selectedType == null) return 0;
    final base = _product!.productPrice + _selectedType!.extraPrice;
    final discount = 1 - (_selectedType!.promotion / 100);
    return base * discount;
  }

  double _getFinalPrice() {
    return _getBasePrice() * _quantity;
  }

  Future<void> _handleAddToCart() async {
    if (_selectedType == null) return;

    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final accountId = user?.accountId ?? '';

    final itemPrice = _getBasePrice();

    final response = await apiService.addToCart(accountId, [
      {
        'ProductID': widget.productId,
        'ProductDetailID': _selectedType!.productDetailId,
        'ItemPrice': itemPrice,
        'ItemQuantity': _quantity,
      },
    ]);

    if (response['errCode'] == 0) {
      Fluttertoast.showToast(msg: 'Đã thêm vào giỏ hàng');
      Provider.of<CartProvider>(context, listen: false).loadCartCount(context);
      _resetSelection();
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Lỗi thêm vào giỏ',
        backgroundColor: Colors.red,
      );
    }
  }

  void _resetSelection() {
    setState(() {
      _quantity = 1;
      _selectedType = _product!.productTypes.isNotEmpty
          ? _product!.productTypes[0]
          : null;
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(color: Color(0xFF4CAF50)),
        ),
      );
    }
    if (_product == null) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Chi Tiết Sản Phẩm'),
          backgroundColor: const Color.fromARGB(255, 237, 237, 237),
        ),
        body: const Center(child: Text('Không tìm thấy sản phẩm')),
      );
    }
    return Scaffold(
      appBar: AppBar(
        title: const Text('Thông tin Sản Phẩm'),
        backgroundColor: const Color.fromARGB(255, 248, 249, 250),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20.0),
        child: Builder(
          builder: (context) {
            double screenWidth = MediaQuery.of(context).size.width;
            Widget leftSection = screenWidth > 768
                ? _buildLargeLeftSection()
                : _buildSmallLeftSection();
            Widget rightSection = _buildRightSection();
            if (screenWidth > 768) {
              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: leftSection),
                  Expanded(child: rightSection),
                ],
              );
            } else {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [leftSection, rightSection],
              );
            }
          },
        ),
      ),
    );
  }

  Widget _buildLargeLeftSection() {
    return Row(
      children: [
        Expanded(
          flex: 8,
          child: Padding(
            padding: const EdgeInsets.only(right: 20.0),
            child: Image.network(
              _selectedImage,
              height: 300,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => const Icon(Icons.error, size: 100),
            ),
          ),
        ),
        Expanded(
          flex: 2,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.start,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildThumbnail(_product!.productImage),
              ..._product!.images.map(
                (img) => Padding(
                  padding: const EdgeInsets.only(top: 10.0),
                  child: _buildThumbnail(img),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildSmallLeftSection() {
    return Column(
      children: [
        Image.network(
          _selectedImage,
          height: 300,
          width: double.infinity,
          fit: BoxFit.cover,
          errorBuilder: (_, __, ___) => const Icon(Icons.error, size: 100),
        ),
        const SizedBox(height: 10),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              _buildThumbnail(_product!.productImage),
              ..._product!.images.map(
                (img) => Padding(
                  padding: const EdgeInsets.only(left: 8.0),
                  child: _buildThumbnail(img),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildRightSection() {
    final basePrice = _getBasePrice();
    final finalPrice = _getFinalPrice();
    return Padding(
      padding: const EdgeInsets.only(left: 20.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            _product!.productName,
            style: const TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.bold,
              color: Color(0xFF333333),
            ),
          ),
          const SizedBox(height: 8),
          if (_selectedType != null)
            Row(
              children: [
                RichText(
                  text: TextSpan(
                    children: [
                      TextSpan(
                        text: basePrice.toStringAsFixed(0),
                        style: const TextStyle(
                          fontSize: 26,
                          fontWeight: FontWeight.bold,
                          color: Colors.red,
                        ),
                      ),
                      WidgetSpan(
                        child: Transform.translate(
                          offset: const Offset(2, -8),
                          child: const Text(
                            'đ',
                            style: TextStyle(fontSize: 16, color: Colors.red),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                if (_selectedType!.promotion > 0)
                  Text(
                    ' (${_selectedType!.promotion}%)',
                    style: const TextStyle(color: Colors.red, fontSize: 18),
                  ),
              ],
            ),
          const SizedBox(height: 10),
          const Text('Chọn loại:', style: TextStyle(fontSize: 18)),
          if (_product!.productTypes.isEmpty)
            const Text('Không có loại nào')
          else
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _product!.productTypes.map((type) {
                final isSelected = _selectedType == type;
                return ElevatedButton(
                  onPressed: () => _handleTypeChange(type),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: isSelected
                        ? const Color.fromRGBO(71, 64, 64, 1)
                        : const Color(0xFFF2F2F2),
                    foregroundColor: isSelected ? Colors.white : Colors.black,
                    side: BorderSide(
                      color: isSelected
                          ? const Color.fromRGBO(71, 64, 64, 1)
                          : const Color.fromRGBO(91, 82, 82, 1),
                      width: 1,
                    ),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 6,
                      vertical: 5,
                    ),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(5),
                    ),
                    elevation: 0,
                  ),
                  child: Text(
                    type.detailName,
                    style: const TextStyle(fontSize: 18),
                  ),
                );
              }).toList(),
            ),
          const SizedBox(height: 20),
          if (_selectedType != null) ...[
            const Text('Số lượng:', style: TextStyle(fontSize: 18)),
            const SizedBox(height: 5),
            Container(
              decoration: BoxDecoration(
                border: Border.all(color: const Color(0xFFDDDDDD)),
                borderRadius: BorderRadius.circular(5),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                    onPressed: _handleQuantityDecrease,
                    icon: const Icon(Ionicons.remove),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(
                      minWidth: 40,
                      minHeight: 40,
                    ),
                    style: ButtonStyle(
                      backgroundColor:
                          MaterialStateProperty.resolveWith<Color?>((states) {
                            if (states.contains(MaterialState.pressed))
                              return const Color(0xFFDEE2E6);
                            if (states.contains(MaterialState.hovered))
                              return const Color(0xFFE9ECEF);
                            return const Color(0xFFF8F9FA);
                          }),
                    ),
                  ),
                  SizedBox(
                    width: 50,
                    child: TextField(
                      textAlign: TextAlign.center,
                      keyboardType: TextInputType.number,
                      onChanged: (value) {
                        int? v = int.tryParse(value);
                        setState(() {
                          _quantity = v != null
                              ? v.clamp(1, _selectedType!.stock)
                              : 1;
                        });
                      },
                      decoration: const InputDecoration(
                        border: InputBorder.none,
                        contentPadding: EdgeInsets.zero,
                      ),
                      style: const TextStyle(fontSize: 20),
                      controller: TextEditingController(
                        text: _quantity.toString(),
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: _handleQuantityIncrease,
                    icon: const Icon(Ionicons.add),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(
                      minWidth: 40,
                      minHeight: 40,
                    ),
                    style: ButtonStyle(
                      backgroundColor:
                          MaterialStateProperty.resolveWith<Color?>((states) {
                            if (states.contains(MaterialState.pressed))
                              return const Color(0xFFDEE2E6);
                            if (states.contains(MaterialState.hovered))
                              return const Color(0xFFE9ECEF);
                            return const Color(0xFFF8F9FA);
                          }),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 10),
            Text(
              'Kho: ${_selectedType!.stock}',
              style: const TextStyle(fontSize: 16),
            ),
            const SizedBox(height: 10),
            RichText(
              text: TextSpan(
                text: 'Tổng tiền: ',
                style: const TextStyle(fontSize: 18, color: Colors.black),
                children: [
                  TextSpan(
                    text: finalPrice.toStringAsFixed(0),
                    style: const TextStyle(
                      fontSize: 18,
                      color: Colors.red,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  WidgetSpan(
                    child: Transform.translate(
                      offset: const Offset(2, -6),
                      child: const Text(
                        'đ',
                        style: TextStyle(fontSize: 14, color: Colors.red),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _handleAddToCart,
              style: ButtonStyle(
                backgroundColor: MaterialStateProperty.resolveWith<Color?>((
                  states,
                ) {
                  if (states.contains(MaterialState.pressed))
                    return const Color.fromRGBO(198, 1, 1, 1);
                  if (states.contains(MaterialState.hovered)) return Colors.red;
                  return const Color(0xFFCCCCCC);
                }),
                foregroundColor: MaterialStateProperty.resolveWith<Color?>((
                  states,
                ) {
                  if (states.contains(MaterialState.hovered) ||
                      states.contains(MaterialState.pressed))
                    return Colors.white;
                  return Colors.black;
                }),
                minimumSize: MaterialStateProperty.all(const Size(160, 40)),
                shape: MaterialStateProperty.all(
                  RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(5),
                  ),
                ),
              ),
              child: const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text('Thêm vào giỏ', style: TextStyle(fontSize: 16)),
                  SizedBox(width: 5),
                  Icon(Ionicons.cart_outline, size: 25),
                ],
              ),
            ),
          ] else
            const Text('Vui lòng chọn loại sản phẩm'),
          const SizedBox(height: 20),
          const Text(
            '*Mô tả sản phẩm:',
            style: TextStyle(
              fontSize: 18,
              decoration: TextDecoration.underline,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            _product!.productDescription ?? 'Không có mô tả',
            style: const TextStyle(
              fontSize: 16,
              color: Color(0xFF666666),
              height: 1.5,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildThumbnail(String image) {
    return GestureDetector(
      onTap: () => _handleImageClick(image),
      child: Container(
        margin: const EdgeInsets.only(right: 8),
        decoration: BoxDecoration(
          border: Border.all(
            color: _selectedImage == image
                ? const Color(0xFF4CAF50)
                : Colors.transparent,
            width: 2,
          ),
        ),
        child: Image.network(image, height: 60, width: 60, fit: BoxFit.cover),
      ),
    );
  }
}
