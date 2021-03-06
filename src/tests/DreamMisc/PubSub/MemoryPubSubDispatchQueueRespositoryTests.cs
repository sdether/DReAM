﻿/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class MemoryPubSubDispatchQueueRespositoryTests {
        private MemoryPubSubDispatchQueueRepository _repository;

        [SetUp]
        public void Setup() {
            CreateRepository();
        }

        [Test]
        public void Unknown_set_returns_null_as_its_dispatch_queue() {

            // Arrange
            var set = CreateSet();

            // Act
            var queue = _repository[set];

            //Assert
            Assert.IsNull(queue, "queue for set was not null");
        }

        [Test]
        public void Can_register_a_new_set_and_retrieve_its_queue() {
            // Arrange
            Func<DispatchItem, Result<bool>> handler = (item) => new Result<bool>().WithReturn(true);
            _repository.InitializeRepository(handler);
            var set = CreateSet();

            // Act
            _repository.RegisterOrUpdate(set);
            var queue = _repository[set];

            // Assert
            Assert.IsNotNull(queue, "didn't get a queue for registered set");
        }

        [Test]
        public void Registered_set_uses_dequeue_handler_from_init() {

            // Arrange
            var dispatched = new List<DispatchItem>();
            Func<DispatchItem, Result<bool>> successHandler = (item) => {
                dispatched.Add(item);
                return new Result<bool>().WithReturn(true);
            };
            _repository.InitializeRepository(successHandler);
            var set = CreateSet();
            var dispatchItem = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");

            // Act
            _repository.RegisterOrUpdate(set);
            _repository[set].Enqueue(dispatchItem);

            // Assert
            Assert.IsTrue(Wait.For(() => dispatched.Count > 0, 10.Seconds()), "no items were dispatched");
            Assert.AreEqual(dispatchItem.Location, dispatched[0].Location, "wrong item location for first dispatched item");
        }

        [Test]
        public void Can_update_set() {

            // Arrange
            Func<DispatchItem, Result<bool>> handler = (item) => new Result<bool>().WithReturn(true);
            _repository.InitializeRepository(handler);
            var set = CreateSet();
            _repository.RegisterOrUpdate(set);

            // Act
            var updated = CreateSet(set.Location);
            _repository.RegisterOrUpdate(updated);

            // Assert
            
            // only condition here is that it doesn't throw.
        }

        [Test]
        public void Deleting_set_removes_it_from_repository() {

            // Arrange
            Func<DispatchItem, Result<bool>> handler = (item) => new Result<bool>().WithReturn(true);
            _repository.InitializeRepository(handler);
            var set = CreateSet();
            _repository.RegisterOrUpdate(set);

            // Act
            _repository.Delete(set);

            // Assert
            Assert.IsNull(_repository[set],"deleted set still had a queue");
        }

        private void CreateRepository() {
            _repository = new MemoryPubSubDispatchQueueRepository(TaskTimerFactory.Current, 10.Seconds());
        }

        private PubSubSubscriptionSet CreateSet() {
            return CreateSet(StringUtil.CreateAlphaNumericKey(4));
        }

        private PubSubSubscriptionSet CreateSet(string location) {
            var setDoc = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            return new PubSubSubscriptionSet(setDoc, location, StringUtil.CreateAlphaNumericKey(4));
        }

    }
}
