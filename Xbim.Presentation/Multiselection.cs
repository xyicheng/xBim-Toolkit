using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Xbim.XbimExtensions.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Xbim.Presentation
{
    class Multiselection : INotifyCollectionChanged, IEnumerable<IPersistIfcEntity>
    {

        private List<SelectionEvent> _selectionLog = new List<SelectionEvent>();
        private List<IPersistIfcEntity> _selection = new List<IPersistIfcEntity>();
        private int position = -1;

        public void Undo()
        {
            if (position >= 0)
            {
                RollBack(_selectionLog[position]);
                position--;
            }
        }

        public void Redo()
        {
            if (position < _selectionLog.Count - 1)
            { 
                position++;
                RollForward(_selectionLog[position]);
            }
        }

        private void RollBack(SelectionEvent e) 
        {
            switch (e.Action)
            {
                case Action.ADD:
                    RemoveRange(e.Entities);
                    break;
                case Action.REMOVE:
                    AddRange(e.Entities);
                    break;
                default:
                    break;
            }
        }

        private void RollForward(SelectionEvent e)
        {
            switch (e.Action)
            {
                case Action.ADD:
                    AddRange(e.Entities);
                    break;
                case Action.REMOVE:
                    RemoveRange(e.Entities);
                    break;
                default:
                    break;
            }
        }

        //add without logging
        private IEnumerable<IPersistIfcEntity> AddRange(IEnumerable<IPersistIfcEntity> entities)
        { 
            List<IPersistIfcEntity> check = new List<IPersistIfcEntity>();
            
            foreach (var item in entities) //check all for redundancy
            {
                if (!_selection.Contains(item))
                {
                    check.Add(item);
                }
            }

            _selection.AddRange(check);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, check);
            return check;
        }

        //remove without logging
        private IEnumerable<IPersistIfcEntity> RemoveRange(IEnumerable<IPersistIfcEntity> entities)
        {
            List<IPersistIfcEntity> check = new List<IPersistIfcEntity>();

            foreach (var item in entities) //check all for existance
            {
                if (_selection.Contains(item))
                {
                    check.Add(item);
                    _selection.Remove(item);
                }
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, check);
            return check;
        }

        public void Add(IPersistIfcEntity entity)
        {
            Add(new IPersistIfcEntity[] { entity });
        }

        public void Add(IEnumerable<IPersistIfcEntity> entity)
        {
            IEnumerable<IPersistIfcEntity> check = AddRange(entity);
            _selectionLog.Add(new SelectionEvent() { Action = Action.ADD, Entities = check });
            ResetLog();
        }


        public void Remove(IPersistIfcEntity entity)
        {
            Remove(new IPersistIfcEntity[] { entity });
        }

        public void Remove(IEnumerable<IPersistIfcEntity> entity)
        {
            IEnumerable<IPersistIfcEntity> check = RemoveRange(entity);
            _selectionLog.Add(new SelectionEvent() { Action = Action.REMOVE, Entities = check });
            ResetLog();
        }

        private void ResetLog()
        {
            if (position == _selectionLog.Count - 2) position = _selectionLog.Count - 1; //normal transaction
            if (position < _selectionLog.Count - 2) //there were undo/redo operations and action inbetween must be discarded
            {
                _selectionLog.RemoveRange(position + 1, _selectionLog.Count - 2);
                position = _selectionLog.Count - 1;
            }
        }

        //    _selectionLog.Add(new SelectionEvent() {Action = Action.ADD, Entities = check});

        private enum Action
        {
            ADD,
            REMOVE
        }

        private struct SelectionEvent
        {
            public Action Action;
            public IEnumerable<IPersistIfcEntity> Entities;
        }


        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<IPersistIfcEntity> entities)
        {
            if (action != NotifyCollectionChangedAction.Add && action != NotifyCollectionChangedAction.Remove)
                throw new ArgumentException("Only Add and Remove operations are supported");
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, entities));
            }
        }

        public IEnumerator<IPersistIfcEntity> GetEnumerator()
        {
            return new SelectionEnumerator(_selection);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
    }

    public class SelectionEnumerator : IEnumerator<IPersistIfcEntity>
    {
        IPersistIfcEntity[] _selection;
        int position = -1;

        public SelectionEnumerator(List<IPersistIfcEntity> selection)
        {
            _selection = selection.ToArray();
        }

        public IPersistIfcEntity Current
        {
            get
            {
                try
                {
                    return _selection[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public void Dispose()
        {
        }

        object IEnumerator.Current
        {
            get { return this.Current; }
        }

  

        public bool MoveNext()
        {
            position++;
            return (position < _selection.Length);
        }

        public void Reset()
        {
            position = -1;
        }
    }

}
