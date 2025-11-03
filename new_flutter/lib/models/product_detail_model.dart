class ProductDetailModel {
  final String productId;
  final String productName;
  final double productPrice;
  final String productImage;
  final String? productDescription;
  final List<ProductTypeModel> productTypes;
  final List<String> images;
  ProductDetailModel({
    required this.productId,
    required this.productName,
    required this.productPrice,
    required this.productImage,
    this.productDescription,
    required this.productTypes,
    required this.images,
  });
  factory ProductDetailModel.fromJson(Map<String, dynamic> json) {
    final detailList = (json['ProductDetail'] as List<dynamic>?) ?? [];
    final imageList = (json['Image'] as List<dynamic>?) ?? [];
    return ProductDetailModel(
      productId: json['ProductID'] ?? '',
      productName: json['ProductName'] ?? '',
      productPrice: double.tryParse(json['ProductPrice'].toString()) ?? 0.0,
      productImage: json['ProductImage'] ?? '',
      productDescription: json['ProductDescription'] ?? 'N/A',
      productTypes: detailList
          .map((d) => ProductTypeModel.fromJson(d))
          .toList(),
      images: imageList.map((img) => img['Image'] as String? ?? '').toList(),
    );
  }
}

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
      detailName: json['DetailName'] ?? 'Mặc định',
      stock: json['Stock'] ?? 0,
      extraPrice: double.tryParse(json['ExtraPrice'].toString()) ?? 0.0,
      promotion: double.tryParse(json['Promotion'].toString()) ?? 0.0,
    );
  }
}
