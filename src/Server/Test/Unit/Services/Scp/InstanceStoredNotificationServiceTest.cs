﻿/*
 * Apache License, Version 2.0
 * Copyright 2019-2020 NVIDIA Corporation
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
using System.IO.Abstractions.TestingHelpers;
using Dicom;
using Dicom.Network;
using Microsoft.Extensions.Logging;
using Moq;
using Nvidia.Clara.DicomAdapter.API;
using Nvidia.Clara.DicomAdapter.Common;
using Nvidia.Clara.DicomAdapter.Server.Services.Disk;
using Nvidia.Clara.DicomAdapter.Server.Services.Scp;
using xRetry;
using Xunit;

namespace Nvidia.Clara.DicomAdapter.Test.Unit
{
    public class InstanceStoredNotificationServiceTest
    {
        private Mock<IInstanceCleanupQueue> _cleanupQueue;
        private Mock<ILogger<InstanceStoredNotificationService>> _logger;

        public InstanceStoredNotificationServiceTest()
        {
            _cleanupQueue = new Mock<IInstanceCleanupQueue>();
            _logger = new Mock<ILogger<InstanceStoredNotificationService>>();
        }

        [RetryFact(DisplayName = "Workflow Test")]
        public void WorkflowTest()
        {
            var service = new InstanceStoredNotificationService(_logger.Object, _cleanupQueue.Object);
            var observer = new Mock<IObserver<InstanceStorageInfo>>();
            observer.Setup(p => p.OnNext(It.IsAny<InstanceStorageInfo>()));

            var cancel = service.Subscribe(observer.Object);
            var request = GenerateRequest();
            var instance = InstanceStorageInfo.CreateInstanceStorageInfo(request, "/storage", "AET", new MockFileSystem());
            service.NewInstanceStored(instance);
            service.NewInstanceStored(instance);
            service.NewInstanceStored(instance);

            observer.Verify(p => p.OnNext(It.IsAny<InstanceStorageInfo>()), Times.Exactly(3));

            cancel.Dispose();
            observer.Reset();
            service.NewInstanceStored(instance);
            observer.Verify(p => p.OnNext(It.IsAny<InstanceStorageInfo>()), Times.Never());
        }
        

        [RetryFact(DisplayName = "NewInstanceStored - no supported observers")]
        public void NewInstanceStored_NoSupportedObservers()
        {
            var request = GenerateRequest();
            var instance = InstanceStorageInfo.CreateInstanceStorageInfo(request, "/storage", "AET", new MockFileSystem());

            _cleanupQueue.Setup(p => p.QueueInstance(It.IsAny<InstanceStorageInfo>()));

            var service = new InstanceStoredNotificationService(_logger.Object, _cleanupQueue.Object);            
            var observer = new Mock<IObserver<InstanceStorageInfo>>();            
            observer.Setup(p => p.OnNext(It.IsAny<InstanceStorageInfo>())).Throws(new InstanceNotSupportedException(instance));

            var cancel = service.Subscribe(observer.Object);
            service.NewInstanceStored(instance);

            observer.Verify(p => p.OnNext(It.IsAny<InstanceStorageInfo>()), Times.Once());
            _cleanupQueue.Verify(p => p.QueueInstance(instance), Times.Once());

            cancel.Dispose();
            observer.Reset();
            service.NewInstanceStored(instance);
            observer.Verify(p => p.OnNext(It.IsAny<InstanceStorageInfo>()), Times.Never());
        }

        private DicomCStoreRequest GenerateRequest()
        {
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.PatientID, "PID");
            dataset.Add(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.Add(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID);
            var file = new DicomFile(dataset);
            return new DicomCStoreRequest(file);
        }
    }
}
