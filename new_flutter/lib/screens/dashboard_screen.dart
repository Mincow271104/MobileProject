import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:ionicons/ionicons.dart';
import '../providers/user_provider.dart';
import '../providers/cart_provider.dart'; // Add this import
import 'cart_screen.dart';
import 'userpage_screen.dart';
import 'home_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  int _selectedIndex = 0;

  static const List<Widget> _widgetOptions = <Widget>[
    HomeScreen(),
    CartScreen(),
    UserpageScreen(),
  ];

  @override
  void initState() {
    super.initState();
    Provider.of<CartProvider>(context, listen: false).loadCartCount(context);
  }

  void _onItemTapped(int index) {
    setState(() {
      _selectedIndex = index;
    });

    if (index == 1) {
      Provider.of<CartProvider>(context, listen: false).loadCartCount(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    final userProvider = Provider.of<UserProvider>(context);
    final cartProvider = Provider.of<CartProvider>(
      context,
    ); // Listen to CartProvider
    final user = userProvider.user;

    return Scaffold(
      // appBar: AppBar(
      //   backgroundColor: const Color(0xFF4CAF50),
      //   actions: [
      //     if (user != null)
      //       IconButton(
      //         icon: const Icon(Ionicons.log_out_outline, color: Colors.white),
      //         onPressed: () async {
      //           await userProvider.logoutUser();
      //           Navigator.pushReplacementNamed(context, '/login');
      //         },
      //       ),
      //   ],
      // ),
      body: Center(child: _widgetOptions.elementAt(_selectedIndex)),
      bottomNavigationBar: BottomNavigationBar(
        items: <BottomNavigationBarItem>[
          const BottomNavigationBarItem(
            icon: Icon(Ionicons.home_outline),
            label: 'Home',
          ),
          BottomNavigationBarItem(
            icon: Stack(
              children: [
                const Icon(Ionicons.cart_outline),
                if (cartProvider.cartItemCount >
                    0) // Use cartProvider.cartItemCount
                  Positioned(
                    right: 0,
                    top: 0,
                    child: Container(
                      padding: const EdgeInsets.all(1),
                      decoration: BoxDecoration(
                        color: Colors.red,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      constraints: const BoxConstraints(
                        minWidth: 16,
                        minHeight: 16,
                      ),
                      child: Text(
                        '${cartProvider.cartItemCount}',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                        textAlign: TextAlign.center,
                      ),
                    ),
                  ),
              ],
            ),
            label: 'Cart',
          ),
          const BottomNavigationBarItem(
            icon: Icon(Ionicons.person_outline),
            label: 'User',
          ),
        ],
        currentIndex: _selectedIndex,
        selectedItemColor: const Color.fromRGBO(91, 85, 85, 1),
        selectedLabelStyle: const TextStyle(fontWeight: FontWeight.bold),
        onTap: _onItemTapped,
        backgroundColor: Colors.white,
        elevation: 5,
      ),
    );
  }
}
