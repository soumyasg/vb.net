Imports System.IO
Module Module1

    Public Const USE_PARALLEL As Boolean = False

    Private tFixed As Double = 0
    Private tList As Double = 0
    'Private tSelected As Double = 0
    Private tCombined As Double = 0


    Sub Main()

        Dim _photoList As New List(Of Photo) '({New Photo(), New Photo(), New Photo()})

        For i As Integer = 0 To 2
            Call _photoList.Add(New Photo() With {.ID = (i + 1).ToString})
        Next

        Dim allActions As New List(Of Action(Of Module1.Photo))({New Action(Of Photo)(AddressOf Photo.ApplyWBFix) _
                                                                , New Action(Of Photo)(AddressOf Photo.ApplyExpFix) _
                                                                , New Action(Of Photo)(AddressOf Photo.ApplyColorFix)})

        Dim selectedActions As New List(Of Action(Of Module1.Photo))({New Action(Of Photo)(AddressOf Photo.ApplyWBFix) _
                                                                  , New Action(Of Photo)(AddressOf Photo.ApplyColorFix)})

        Dim combinedAction As Action(Of Photo) = Nothing
        Call allActions.ForEach(Sub(p)
                                    combinedAction = IIf((combinedAction Is Nothing), p, [Delegate].Combine(combinedAction, p))
                                End Sub)



        Do

            Console.WriteLine()
            Dim t = Task.Run(Of List(Of Photo))(Function()
                                                    Return PhotoProcessor.GetProcessedPhotos(_photoList)
                                                End Function)

            t = t.ContinueWith(Of List(Of Photo))(Function()
                                                      Return PhotoProcessor.GetProcessedPhotos(_photoList, allActions)
                                                  End Function)

            t = t.ContinueWith(Of List(Of Photo))(Function()
                                                      Return PhotoProcessor.GetProcessedPhotos(_photoList, combinedAction)
                                                  End Function)



            ''Call PhotoProcessor.GetProcessedPhotos(_photoList)
            ''Console.WriteLine(New String("*"c, 30))
            ''Call PhotoProcessor.GetProcessedPhotos(_photoList, allActions)
            ''Console.WriteLine(New String("*"c, 30))
            ''''Call PhotoProcessor.GetProcessedPhotos(_photoList, selectedActions)
            ''''Console.WriteLine(New String("*"c, 30))
            ''Call PhotoProcessor.GetProcessedPhotos(_photoList, combinedAction)
            ''t.Wait()

            Console.WriteLine("Return from Task")
            Console.WriteLine(New String("-"c, 30))
            t.Result.ForEach(Sub(p)
                                 Console.WriteLine(p)
                             End Sub)
            Console.WriteLine(New String("-"c, 30))

            't.Wait()

            If USE_PARALLEL Then
                Console.WriteLine("Parallel Time taken- Fixed: {0} DelegateList: {1} DelegateCombine: {2}", tFixed, tList, tCombined)
            Else
                Console.WriteLine("Synch Time taken- Fixed: {0} DelegateList: {1} DelegateCombine: {2}", tFixed, tList, tCombined)
            End If

            Console.WriteLine("Do it again? (Y/N): ")
            'Dim key = Console.ReadKey()

        Loop While ("Yy".Contains(Console.ReadKey.KeyChar))

        Do
            Console.WriteLine()
            Console.WriteLine("Enter root directory")
            Dim rootPath As String = Console.ReadLine
            Try
                If Directory.Exists(rootPath) Then
                    Call DirectoryTraverser.TraverseDirectory(rootPath, Function(s)
                                                                            Console.WriteLine(s)
                                                                            Return s.Contains("Temp")
                                                                        End Function)
                Else
                    Console.WriteLine("Invalid directory")
                End If
            Catch ex As Exception
                Console.WriteLine(ex.Message)
            End Try

            Console.WriteLine("Do it again? (Y/N): ")
        Loop While ("Yy".Contains(Console.ReadKey.KeyChar))


    End Sub

    Private sw As New Stopwatch

    Public Class PhotoProcessor

        Public Shared Function GetProcessedPhotos(photoList As List(Of Photo)) As List(Of Photo)
            '***********************************************************************
            'This version performs a hardcoded list of operations on each Photo
            'This implementation is 'rigid' and is not reusable.
            '*************************************************************************
            'It applies white color fix
            'It applies Exposure Fix
            'It applies color fix

            sw.Restart()

            If USE_PARALLEL Then
                Parallel.ForEach(Of Photo)(photoList, Sub(p)
                                                          Call Photo.ApplyWBFix(p)
                                                          Call Photo.ApplyExpFix(p)
                                                          Call Photo.ApplyColorFix(p)
                                                      End Sub)
            Else
                photoList.ForEach(Sub(p)
                                      Call Photo.ApplyWBFix(p)
                                      Call Photo.ApplyExpFix(p)
                                      Call Photo.ApplyColorFix(p)
                                  End Sub)
            End If

            Console.WriteLine(New String("*"c, 30))

            sw.Stop()

            tFixed = sw.ElapsedMilliseconds

            'Console.WriteLine("Time taken: {0}", sw.ElapsedMilliseconds.ToString)



            Return photoList

        End Function

        Public Shared Function GetProcessedPhotos(photoList As List(Of Photo), actionList As List(Of Action(Of Photo))) As List(Of Photo)

            '************************************************************************************
            'This version loops through a list of delegates and invokes them on each Photo
            'Reusable
            '************************************************************************************
            sw.Restart()

            If USE_PARALLEL Then
                Parallel.ForEach(Of Photo)(photoList, Sub(p)
                                                          Parallel.ForEach(Of Action(Of Photo))(actionList, Sub(n)
                                                                                                                Call n(p)
                                                                                                            End Sub)

                                                      End Sub)
            Else
                photoList.ForEach(Sub(p)
                                      actionList.ForEach(Sub(n)
                                                             Call n(p)
                                                         End Sub)
                                  End Sub)
            End If

            Console.WriteLine(New String("*"c, 30))

            sw.Stop()

            tList = sw.ElapsedMilliseconds

            'Console.WriteLine("Time taken: {0}", sw.ElapsedMilliseconds.ToString)

            Return photoList

        End Function

        Public Shared Function GetProcessedPhotos(photoList As List(Of Photo), action As Action(Of Photo)) As List(Of Photo)
            '************************************************************************************
            'This version invokes the passed in delegate
            'Reusable
            '************************************************************************************
            sw.Restart()
            Dim counter As Integer = 1
            Console.WriteLine("Invocation List:")
            Console.WriteLine(New String("-"c, 30))
            Array.ForEach(Of [Delegate])(action.GetInvocationList, Sub(p)
                                                                       Console.WriteLine(counter.ToString & ": " & p.Method.Name) : counter += 1
                                                                   End Sub)
            Console.WriteLine(New String("-"c, 30))
            If USE_PARALLEL Then
                Parallel.ForEach(Of Photo)(photoList, Sub(p)
                                                          action(p)
                                                      End Sub)
            Else
                photoList.ForEach(Sub(p)
                                      action(p)
                                  End Sub)
            End If

            Console.WriteLine(New String("*"c, 30))

            sw.Stop()
            tCombined = sw.ElapsedMilliseconds

            'Console.WriteLine("Time taken: {0}", sw.ElapsedMilliseconds.ToString)

            Return photoList

        End Function

    End Class

    Public Class Photo
        Public Property ID As String

        Public Shared Sub ApplyWBFix(photo As Photo)
            Call Console.WriteLine("1. Applying WB Fix")
        End Sub

        Public Shared Sub ApplyExpFix(photo As Photo)
            Call Console.WriteLine("2. Applying Exp Fix")
        End Sub

        Public Shared Sub ApplyColorFix(photo As Photo)
            Call Console.WriteLine("3. Applying Color Fix")
        End Sub

        Public Overrides Function ToString() As String
            Return "Photo #" & ID
        End Function
    End Class

    Public Class DirectoryTraverser
        ''A delegate that will display the directory name and also signal if processing should be cancelled
        ''Public Delegate Function DirectoryNameHandler(name As String) As Boolean
        Public Shared Sub TraverseDirectory(rootPath As String, callback As Predicate(Of String))
            For Each path As String In Directory.GetDirectories(rootPath)
                Dim stopLooping As Boolean = callback(path)
                If stopLooping Then Exit For
            Next

        End Sub

    End Class

End Module
