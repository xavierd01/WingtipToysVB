Imports System.ComponentModel.DataAnnotations

Namespace Models
    Public Class CartItem
        <Key> Public Property ItemId() As String

        Public Property CartId() As String

        Public Property Quantity() As Integer

        Public Property DateCreated() As System.DateTime

        Public Property ProductId() As Integer

        Public Overridable Property Product() As Product

    End Class
End Namespace