class ProductTypeModel {
  final int productDetailId;
  final String detailName;
  final int stock;
  final double extraPrice;
  final double promotion;

  ProductTypeModel({
    required this.productDetailId,
    required this.detailName,
    required this.stock,
    required this.extraPrice,
    required this.promotion,
  });

  factory ProductTypeModel.fromJson(Map<String, dynamic> json) {
    return ProductTypeModel(
      productDetailId: json['ProductDetailID'] ?? 0,
      detailName: json['DetailName'] ?? '',
      stock: json['Stock'] ?? 0,
      extraPrice: double.tryParse(json['ExtraPrice'].toString()) ?? 0.0,
      promotion: double.tryParse(json['Promotion'].toString()) ?? 0.0,
    );
  }
}
