﻿using System;
using System.Collections.Generic;

#if BUILD_PEANUTBUTTER_INTERNAL
namespace Imported.PeanutButter.Utils
#else
namespace PeanutButter.Utils
#endif
{
    /// <summary>
    /// Provides methods on lists as one might expect from JavaScript
    /// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
    internal
#else
    public
#endif
        static class ListExtensions
    {
        /// <summary>
        /// Removes the first item from the list and returns it
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when the list is empty</exception>
        /// <returns></returns>
        public static T Shift<T>(
            this IList<T> list
        )
        {
            ValidateListContainsElements(list);
            var result = list[0];
            list.RemoveAt(0);
            return result;
        }

        /// <summary>
        /// Removes the last item from the list and returns it
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when the list is empty</exception>
        /// <returns></returns>
        public static T Pop<T>(
            this IList<T> list
        )
        {
            ValidateListContainsElements(list);
            var idx = list.Count - 1;
            var result = list[idx];
            list.RemoveAt(idx);
            return result;
        }

        /// <summary>
        /// Attempt to pop the last element off of a list
        /// - Returns true and sets the result when the list has elements
        /// - Returns false if the list has no elements to pop
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryPop<T>(
            this IList<T> list,
            out T result
        )
        {
            if (list.Count < 1)
            {
                result = default;
                return false;
            }

            var idx = list.Count - 1;
            result = list[idx];
            list.RemoveAt(idx);
            return true;
        }

        /// <summary>
        /// Attempt to shift the first element off of a list
        /// - Returns true and sets the result when the list has elements
        /// - Returns false if the list has no elements to shift
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryShift<T>(
            this IList<T> list,
            out T result
        )
        {
            if (list.Count < 1)
            {
                result = default;
                return false;
            }
            result = list[0];
            list.RemoveAt(0);
            return true;
        }

        /// <summary>
        /// Inserts an item at the beginning of the list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static void Unshift<T>(
            this IList<T> list,
            T value
        )
        {
            list.Insert(0, value);
        }

        /// <summary>
        /// Alias for .Add: appends an item to the list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static void Push<T>(
            this IList<T> list,
            T value
        )
        {
            list.Add(value);
        }

        /// <summary>
        /// Adds the value to the list if the flag was set to true
        /// shortcut for:
        /// if (flag)
        /// {
        ///     list.Add(value);
        /// }
        /// </summary>
        /// <param name="list"></param>
        /// <param name="shouldAdd"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static void AddIf<T>(
            this IList<T> list,
            bool shouldAdd,
            T value
        )
        {
            if (shouldAdd)
            {
                list.Add(value);
            }
        }

        /// <summary>
        /// Adds all the provided items and returns the list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="items"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IList<T> AddAll<T>(
            this IList<T> list,
            params T[] items
        )
        {
            if (list is List<T> lst)
            {
                // concrete list can do this more efficiently
                lst.AddRange(items);
            }
            else
            {
                foreach (var item in items)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        private static void ValidateListContainsElements<T>(IList<T> list)
        {
            if (list.Count < 1)
            {
                throw new InvalidOperationException("List contains no elements");
            }
        }
    }
}