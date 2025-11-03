class CartItemModel {
  final int cartItemId;
  final String productId;
  final int productDetailId;
  final double itemPrice;
  final int itemQuantity;
  final String productName;
  final String productImage;
  final String detailName;
  final int stock;
  final double promotion;

  CartItemModel({
    required this.cartItemId,
    required this.productId,
    required this.productDetailId,
    required this.itemPrice,
    required this.itemQuantity,
    required this.productName,
    required this.productImage,
    required this.detailName,
    required this.stock,
    required this.promotion,
  });

  factory CartItemModel.fromJson(Map<String, dynamic> json) {
    return CartItemModel(
      cartItemId: json['CartItemID'] ?? 0,
      productId: json['ProductID'] ?? '',
      productDetailId: json['ProductDetailID'] ?? 0,
      itemPrice: double.tryParse(json['ItemPrice'].toString()) ?? 0.0,
      itemQuantity: json['ItemQuantity'] ?? 1,
      productName: json['ProductName'] ?? '',
      productImage: json['ProductImage'] ?? '',
      detailName: json['DetailName'] ?? '',
      stock: json['Stock'] ?? 0,
      promotion: double.tryParse(json['Promotion'].toString()) ?? 0.0,
    );
  }
}
