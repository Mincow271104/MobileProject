class ProductModel {
  final String productId;
  final String productName;
  final double itemPrice;
  final String productImage;
  final int? productDetailId;
  final String? detailName;
  final double? promotion;

  ProductModel({
    required this.productId,
    required this.productName,
    required this.itemPrice,
    required this.productImage,
    this.productDetailId,
    this.detailName,
    this.promotion,
  });

  factory ProductModel.fromJson(Map<String, dynamic> json) {
    return ProductModel(
      productId: json['ProductID'],
      productName: json['ProductName'],
      itemPrice: double.parse(json['ItemPrice']),
      productImage: json['ProductImage'],
      productDetailId: json['ProductDetailID'],
      detailName: json['DetailName'],
      promotion: json['Promotion'] != null
          ? double.parse(json['Promotion'].toString())
          : null,
    );
  }
}
