Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports WingtipToysVB.Models

Namespace Logic
    Public Class ShoppingCartActions
        Implements IDisposable

        Public Property ShoppingCartId() As String

        Private _db As ProductContext = New ProductContext()

        Public Const CartSessionKey As String = "CartId"

        Public Sub AddToCart(id As Integer)
            ' Retrieve the product from the database
            ShoppingCartId = GetCartId()

            Dim cartItem = _db.ShoppingCartItems.SingleOrDefault(
                Function(c) c.CartId = ShoppingCartId AndAlso c.ProductId = id)

            If cartItem Is Nothing Then
                ' Create a new cart item if no cart item exists.
                cartItem = New CartItem With
                    {
                        .ItemId = Guid.NewGuid().ToString(),
                        .ProductId = id,
                        .CartId = ShoppingCartId,
                        .Product = _db.Products.SingleOrDefault(
                                        Function(p) p.ProductID = id),
                        .Quantity = 1,
                        .DateCreated = DateTime.Now
                    }
                _db.ShoppingCartItems.Add(cartItem)

            Else
                ' If the item does exist in the cart,
                ' then add one to the quantity.
                cartItem.Quantity += 1
            End If
            _db.SaveChanges()
        End Sub

        Public Function GetCartId() As String
            If HttpContext.Current.Session(CartSessionKey) Is Nothing Then
                If Not String.IsNullOrWhiteSpace(HttpContext.Current.User.Identity.Name) Then
                    HttpContext.Current.Session(CartSessionKey) = HttpContext.Current.User.Identity.Name
                Else
                    ' Generate a new random GUID using System.Guid class.
                    Dim tempCartId = Guid.NewGuid()
                    HttpContext.Current.Session(CartSessionKey) = tempCartId.ToString()
                End If
            End If
            Return HttpContext.Current.Session(CartSessionKey).ToString()
        End Function

        Public Function GetCartItems() As List(Of CartItem)
            ShoppingCartId = GetCartId()

            Return _db.ShoppingCartItems.Where(
                Function(c) c.CartId = ShoppingCartId).ToList()
        End Function

        Public Function GetTotal() As Decimal
            ShoppingCartId = GetCartId()
            ' Multiply product price by quantity of that product to get
            ' the correct price for each of those products in the cart.
            ' Sum all product price totals to get cart total.
            Dim total As Decimal? = Decimal.Zero
            total = CType((From cartItems In _db.ShoppingCartItems
                           Where cartItems.CartId = ShoppingCartId
                           Select CType(cartItems.Quantity, Integer?) *
                            cartItems.Product.UnitPrice).Sum(), Decimal?)
            Return If(total, Decimal.Zero)
        End Function

        Public Function GetCart(context As HttpContext) As ShoppingCartActions
            Using cart As ShoppingCartActions = New ShoppingCartActions()
                cart.ShoppingCartId = cart.GetCartId()
                Return cart
            End Using
        End Function

        Public Sub UpdateShoppingCartDatabase(cartId As String, cartItemUpdates() As ShoppingCartUpdates)
            Using db = New WingtipToysVB.Models.ProductContext()
                Try
                    Dim cartItemCount = cartItemUpdates.Count()
                    Dim myCart As List(Of CartItem) = GetCartItems()
                    For Each cartItem In myCart
                        ' Iterate through all rows within shopping cart list
                        For i As Integer = 0 To cartItemCount - 1
                            If cartItem.ProductId = cartItemUpdates(i).ProductId Then
                                If cartItemUpdates(i).PurchaseQuantity < 1 OrElse cartItemUpdates(i).RemoveItem Then
                                    RemoveItem(cartId, cartItem.ProductId)
                                Else
                                    UpdateItem(cartId, cartItem.ProductId, cartItemUpdates(i).PurchaseQuantity)
                                End If
                            End If
                        Next
                    Next

                Catch ex As Exception
                    Throw New Exception("ERROR: Unable to Update Cart Database - " + ex.Message.ToString(), ex)
                End Try
            End Using
        End Sub

        Public Sub RemoveItem(removeCartId As String, removeProductId As Integer)
            Using _db = New WingtipToysVB.Models.ProductContext()
                Try
                    Dim myItem = (From c In _db.ShoppingCartItems Where c.CartId = removeCartId AndAlso c.ProductId = removeProductId Select c).FirstOrDefault()
                    If myItem IsNot Nothing Then
                        ' Remove item.
                        _db.ShoppingCartItems.Remove(myItem)
                        _db.SaveChanges()
                    End If
                Catch ex As Exception
                    Throw New Exception("ERROR: Unable to Remove Cart Item - " + ex.Message.ToString(), ex)
                End Try
            End Using
        End Sub

        Public Sub UpdateItem(updateCartId As String, updateProductId As String, updatePurchaseQuantity As Integer)
            Using _db = New WingtipToysVB.Models.ProductContext()
                Try
                    Dim myItem = (From c In _db.ShoppingCartItems Where c.CartId = updateCartId AndAlso c.ProductId = updateProductId Select c).FirstOrDefault()
                    If myItem IsNot Nothing Then
                        ' Update Item
                        myItem.Quantity = updatePurchaseQuantity
                        _db.SaveChanges()
                    End If
                Catch ex As Exception
                    Throw New Exception("ERROR: Unable to Update Cart Item - " + ex.Message.ToString(), ex)
                End Try
            End Using
        End Sub

        Public Sub EmptyCart()
            ShoppingCartId = GetCartId()
            Dim cartItems = _db.ShoppingCartItems.Where(Function(c) c.CartId = ShoppingCartId)
            For Each cartItem In cartItems
                _db.ShoppingCartItems.Remove(cartItem)
            Next
            _db.SaveChanges()
        End Sub

        Public Function GetCount() As Integer
            ShoppingCartId = GetCartId()
            ' Get the count of each item in the cart and sum them up
            Dim count As Integer? =
                (From cartItems In _db.ShoppingCartItems
                 Where cartItems.CartId = ShoppingCartId
                 Select CType(cartItems.Quantity, Integer?)).Sum()
            Return If(count, 0)
        End Function

        Public Structure ShoppingCartUpdates
            Public ProductId As Integer
            Public PurchaseQuantity As Integer
            Public RemoveItem As Boolean
        End Structure

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If _db IsNot Nothing Then
                    _db.Dispose()
                    _db = Nothing
                End If
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class
End Namespace
