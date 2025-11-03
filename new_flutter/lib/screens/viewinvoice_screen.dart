// lib/screens/viewinvoice_screen.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:intl/intl.dart';
import 'package:ionicons/ionicons.dart'; // Thêm nếu chưa có, hoặc dùng Icon từ Material
import '../providers/user_provider.dart';
import '../models/invoice_model.dart'; // Import model nếu đã có
import 'invoice_screen.dart'; // Import trang chi tiết hóa đơn

class ViewInvoiceScreen extends StatefulWidget {
  const ViewInvoiceScreen({super.key});

  @override
  State<ViewInvoiceScreen> createState() => _ViewInvoiceScreenState();
}

class _ViewInvoiceScreenState extends State<ViewInvoiceScreen> {
  List<Map<String, dynamic>> _invoices = [];
  bool _isLoading = true;
  int _page = 1;
  int _totalPages = 1;
  int _limit = 5; // Giống limitInvoicePerQuery trong FE
  String _search = '';
  String _filter = 'ALL';
  String _sort = '0';
  String _date1 = '';
  String _date2 = '';

  @override
  void initState() {
    super.initState();
    _loadInvoices();
  }

  Future<void> _loadInvoices() async {
    setState(() => _isLoading = true);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final user = Provider.of<UserProvider>(context, listen: false).user;
    final response = await apiService.getAccountInvoiceInfo(
      user?.accountId ?? '',
    );
    if (response['errCode'] == 0) {
      setState(() {
        _invoices = List<Map<String, dynamic>>.from(response['data']);
        _totalPages = (_invoices.length / _limit).ceil();
        _isLoading = false;
      });
    } else {
      Fluttertoast.showToast(msg: response['errMessage'] ?? 'Lỗi tải hóa đơn');
      setState(() => _isLoading = false);
    }
  }

  void _onInvoiceTap(String invoiceId) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => InvoiceScreen(invoiceId: invoiceId),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(
            valueColor: AlwaysStoppedAnimation<Color>(Colors.blueAccent),
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Lịch sử đơn hàng',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        elevation: 1,
        foregroundColor: Colors.black,
      ),
      body: RefreshIndicator(
        onRefresh: _loadInvoices,
        child: _invoices.isEmpty
            ? const Center(
                child: Text(
                  'Không có đơn hàng nào',
                  style: TextStyle(fontSize: 16, color: Colors.grey),
                ),
              )
            : ListView.builder(
                itemCount: _invoices.length,
                padding: const EdgeInsets.all(8.0),
                itemBuilder: (context, index) {
                  final invoice = _invoices[index];
                  final formattedDate = DateFormat(
                    'dd/MM/yyyy HH:mm',
                  ).format(DateTime.parse(invoice['CreatedAt']));
                  final formattedTotal = NumberFormat('#,### đ').format(
                    double.tryParse(invoice['TotalPayment'].toString()) ?? 0,
                  );

                  return Card(
                    elevation: 2,
                    margin: const EdgeInsets.symmetric(vertical: 4.0),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8.0),
                    ),
                    child: ListTile(
                      leading: Icon(
                        Ionicons.receipt_outline, // Hoặc Icons.receipt
                        color: Colors.blueAccent,
                        size: 40,
                      ),
                      title: Text(
                        'Mã đơn: ${invoice['InvoiceID']}',
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                      subtitle: Text(
                        'Ngày: $formattedDate\nTổng: $formattedTotal',
                        style: const TextStyle(color: Colors.grey),
                      ),
                      trailing: const Icon(Ionicons.chevron_forward),
                      onTap: () => _onInvoiceTap(invoice['InvoiceID']),
                    ),
                  );
                },
              ),
      ),
    );
  }
}
