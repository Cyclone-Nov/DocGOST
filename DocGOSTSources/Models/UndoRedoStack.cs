using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class UndoRedoStack<T>
    {
        private int _stackSize;
        private int _currentIndex = 0;

        private List<T> _items = new List<T>();

        public bool IsUndoEnabled
        {
            get
            {
                return _currentIndex > 0;
            }
        }

        public bool IsRedoEnabled
        {
            get
            {
                return _currentIndex < _items.Count - 1;
            }
        }

        public UndoRedoStack(int aStackSize = 10)
        {
            _stackSize = aStackSize;
        }

        public void Add(T aItem)
        {
            if (_items.Count > 0 && _currentIndex != _items.Count - 1)
            {
                // Remove "future" items
                _items.RemoveRange(_currentIndex + 1, _items.Count - _currentIndex - 1);
            }

            // Add item
            _items.Add(aItem);

            if (_items.Count > _stackSize)
            {
                // Remove oldest item
                _items.RemoveAt(0);
            }

            // Set current index
            _currentIndex = _items.Count - 1;
        }

        public void Clear()
        {
            _items.Clear();
            _currentIndex = 0;
        }

        public T Undo()
        {
            if (--_currentIndex < 0)
            {
                _currentIndex = 0;
                return default(T);
            }
            return _items.ElementAtOrDefault(_currentIndex);
        }

        public T Redo()
        {
            if (++_currentIndex > _items.Count - 1)
            {
                _currentIndex = _items.Count - 1;
                return default(T);
            }
            return _items.ElementAtOrDefault(_currentIndex);
        }        
    }
}
