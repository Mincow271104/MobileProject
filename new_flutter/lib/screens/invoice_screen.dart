import 'package:flutter/material.dart';
import 'package:ionicons/ionicons.dart';
import 'package:provider/provider.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'dart:io';
import 'package:path_provider/path_provider.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:open_file/open_file.dart';
import '../utils/api.dart';
import '../providers/user_provider.dart';
import 'dashboard_screen.dart';

class InvoiceScreen extends StatefulWidget {
  final String invoiceId;
  const InvoiceScreen({super.key, required this.invoiceId});

  @override
  State<InvoiceScreen> createState() => _InvoiceScreenState();
}

class _InvoiceScreenState extends State<InvoiceScreen> {
  Map<String, dynamic>? _invoiceDetails;
  bool _isLoading = true;
  String _email = '';
  bool _isShowCancelModal = false;
  String _cancelReason = '';
  List<Map<String, dynamic>> _cancelReasons = [];

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
    _email = user?.email ?? '';

    // Load cancel reasons from AllCodes (Type = 'CancelReason')
    final reasonsResponse = await apiService.getAllCodes('CancelReason');
    if (reasonsResponse['errCode'] == 0 && reasonsResponse['data'] != null) {
      _cancelReasons = List<Map<String, dynamic>>.from(reasonsResponse['data']);
      _cancelReason = _cancelReasons.isNotEmpty
          ? _cancelReasons[0]['Code']
          : '';
    }

    final response = await apiService.getInvoiceDetailInfo(widget.invoiceId);
    if (response['errCode'] == 0 && response['data'] != null) {
      setState(() {
        _invoiceDetails = response['data'];
        _isLoading = false;
      });
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Không tìm thấy hóa đơn!',
      );
      setState(() => _isLoading = false);
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (context) => const DashboardScreen()),
      );
    }
  }

  Future<void> _sendEmail() async {
    if (_email.isEmpty) {
      Fluttertoast.showToast(msg: 'Vui lòng nhập email!');
      return;
    }
    setState(() => _isLoading = true);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final response = await apiService.sendInvoiceEmail(
      widget.invoiceId,
      _email,
    );
    setState(() => _isLoading = false);
    if (response['errCode'] == 0) {
      Fluttertoast.showToast(msg: 'Gửi email thành công!');
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Gửi email thất bại!',
        backgroundColor: Colors.red,
      );
    }
  }

  Future<void> _generatePDF() async {
    if (_invoiceDetails == null) return;

    final pdf = pw.Document();
    pdf.addPage(
      pw.Page(
        build: (pw.Context context) => pw.Column(
          crossAxisAlignment: pw.CrossAxisAlignment.start,
          children: [
            pw.Text(
              'Hóa Đơn #${widget.invoiceId}',
              style: pw.TextStyle(fontSize: 24, fontWeight: pw.FontWeight.bold),
            ),
            pw.SizedBox(height: 10),
            pw.Text('Thời gian: ${_invoiceDetails!['CreatedAt']}'),
            pw.Text('Người nhận: ${_invoiceDetails!['ReceiverName']}'),
            pw.Text('SĐT: ${_invoiceDetails!['ReceiverPhone']}'),
            pw.Text('Địa chỉ: ${_invoiceDetails!['ReceiverAddress']}'),
            pw.SizedBox(height: 20),
            pw.Table.fromTextArray(
              headers: ['Sản phẩm', 'Loại', 'Giá', 'SL', 'Thành tiền'],
              data: (_invoiceDetails!['ProductList'] as List)
                  .map(
                    (item) => [
                      item['ProductName'],
                      item['DetailName'],
                      item['ItemPrice'].toString(),
                      item['ItemQuantity'].toString(),
                      (item['ItemPrice'] * item['ItemQuantity']).toString(),
                    ],
                  )
                  .toList(),
            ),
            pw.SizedBox(height: 10),
            pw.Text('Tổng: ${_invoiceDetails!['TotalPayment']} đ'),
          ],
        ),
      ),
    );

    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/invoice_${widget.invoiceId}.pdf');
    await file.writeAsBytes(await pdf.save());
    OpenFile.open(file.path);
  }

  Future<void> _cancelInvoice() async {
    if (_cancelReason.isEmpty) {
      Fluttertoast.showToast(msg: 'Vui lòng chọn lý do hủy!');
      return;
    }
    setState(() => _isLoading = true);
    final apiService = Provider.of<UserProvider>(
      context,
      listen: false,
    ).apiService;
    final response = await apiService.changeInvoiceStatus(
      widget.invoiceId,
      'ShippingStatus',
      'PEND_CANCEL',
      _cancelReason,
    );
    setState(() => _isLoading = false);
    if (response['errCode'] == 0) {
      Fluttertoast.showToast(msg: 'Gửi yêu cầu hủy thành công!');
      _isShowCancelModal = false;
      await _loadData();
    } else {
      Fluttertoast.showToast(
        msg: response['errMessage'] ?? 'Hủy thất bại!',
        backgroundColor: Colors.red,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_invoiceDetails == null) {
      return const Scaffold(
        body: Center(child: Text('Không tìm thấy hóa đơn')),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Hóa đơn #${widget.invoiceId}',
          style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
        ),
        backgroundColor: const Color.fromARGB(255, 255, 255, 255),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            const SizedBox(height: 10),
            Card(
              elevation: 4,
              child: Padding(
                padding: const EdgeInsets.all(8.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Thời gian: ${_invoiceDetails!['CreatedAt']}'),
                    Text('Người nhận: ${_invoiceDetails!['ReceiverName']}'),
                    Text('SĐT: ${_invoiceDetails!['ReceiverPhone']}'),
                    Text('Địa chỉ: ${_invoiceDetails!['ReceiverAddress']}'),
                    Text('PT thanh toán: ${_invoiceDetails!['PaymentType']}'),
                    Text('PT giao hàng: ${_invoiceDetails!['ShippingMethod']}'),
                    Text('TT giao hàng: ${_invoiceDetails!['ShippingStatus']}'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),
            // Table sản phẩm
            const Text(
              'Danh sách sản phẩm',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            Table(
              border: TableBorder.all(),
              children: [
                const TableRow(
                  children: [
                    Padding(
                      padding: EdgeInsets.all(8.0),
                      child: Text(
                        'Sản phẩm',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ),
                    Padding(
                      padding: EdgeInsets.all(8.0),
                      child: Text(
                        'Loại',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ),
                    Padding(
                      padding: EdgeInsets.all(8.0),
                      child: Text(
                        'Giá',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ),
                    Padding(
                      padding: EdgeInsets.all(8.0),
                      child: Text(
                        'SL',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ),
                    Padding(
                      padding: EdgeInsets.all(8.0),
                      child: Text(
                        'Thành tiền',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ),
                  ],
                ),
                ...(_invoiceDetails!['ProductList'] as List).map(
                  (item) => TableRow(
                    children: [
                      Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text(item['ProductName']),
                      ),
                      Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text(item['DetailName']),
                      ),
                      Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text(item['ItemPrice'].toString()),
                      ),
                      Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text(item['ItemQuantity'].toString()),
                      ),
                      Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text(
                          (item['ItemPrice'] * item['ItemQuantity']).toString(),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Text(
              'Tổng: ${_invoiceDetails!['TotalPayment']} đ',
              style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: Colors.red,
              ),
            ),
            const SizedBox(height: 20),
            // Nút chức năng
            Row(
              children: [
                Expanded(
                  child: TextField(
                    decoration: const InputDecoration(labelText: 'Email'),
                    onChanged: (value) => _email = value,
                  ),
                ),
                ElevatedButton(
                  onPressed: _sendEmail,
                  child: const Text(
                    'Gửi email',
                    style: TextStyle(color: Color.fromARGB(255, 0, 0, 0)),
                  ),
                ),
                const SizedBox(width: 10),
                ElevatedButton(
                  onPressed: _generatePDF,
                  child: const Text(
                    'Tải PDF',
                    style: TextStyle(color: Color.fromARGB(255, 0, 0, 0)),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),

            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                if (_invoiceDetails!['ShippingStatus'] == 'PEND') ...[
                  ElevatedButton(
                    onPressed: _cancelInvoice,
                    child: const Text(
                      'Hủy đơn',
                      style: TextStyle(color: Color.fromARGB(255, 0, 0, 0)),
                    ),
                  ),
                  const SizedBox(width: 5),
                  const Text('Vì: '),
                  const SizedBox(width: 4),
                  DropdownButton<String>(
                    value: _cancelReason,
                    items: _cancelReasons.map((reason) {
                      return DropdownMenuItem<String>(
                        value: reason['Code'],
                        child: Text(reason['CodeValueVI']),
                      );
                    }).toList(),
                    onChanged: (value) =>
                        setState(() => _cancelReason = value!),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}
