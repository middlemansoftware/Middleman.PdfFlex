// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
#if GDI
using System.Drawing;
using System.Drawing.Drawing2D;
#endif
#if WPF
using System.Windows.Media;
#endif

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Represents a stack of XGraphicsState and XGraphicsContainer objects.
    /// </summary>
    class GraphicsStateStack
    {
        public GraphicsStateStack(XGraphics gfx)
        {
            _current = new InternalGraphicsState(gfx);
        }

        public int Count => _stack.Count;

        public void Push(InternalGraphicsState state)
        {
            _stack.Push(state);
            state.Pushed();
        }

        public int Restore(InternalGraphicsState state)
        {
            if (!_stack.Contains(state))
                throw new ArgumentException("State not on stack.", nameof(state));
            if (state.Invalid)
                throw new ArgumentException("State already restored.", nameof(state));

            int count = 1;
            InternalGraphicsState top = _stack.Pop();
            top.Popped();
            while (top != state)
            {
                count++;
                state.Invalid = true;
                top = _stack.Pop();
                top.Popped();
            }
            state.Invalid = true;
            return count;
        }

        public InternalGraphicsState Current
        {
            get
            {
                if (_stack.Count == 0)
                    return _current;
                return _stack.Peek();
            }
        }

        readonly InternalGraphicsState _current;

        readonly Stack<InternalGraphicsState> _stack = new Stack<InternalGraphicsState>();
    }
}
