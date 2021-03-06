# Changelog

## 0.7.0
* :new: new: DICOM Adapter now accepts concurrent associations per AE Title and has a new Job Processor extension, designed
to allow developers to extend and customize how received DICOM instances can be associated with a pipeline job.
* :warning: breaking: The YAML-formatted configuration file has been replaced and consolidated into a single `appsettings.json` file.



## 0.6.0

* :new: new: configure *Clara AE-Title*s, *Sources and *Destinations* via Kubernetes CRD is added which allows user to add a new Clara AE-Title and 
associate it with a Clara Pipeline without restarting DICOM Adapter.  DICOM sources and destination can also be added via CRD.
* :no_entry: removed: `timeout-group` is no longer supported.  This can be replaced by custom plug-in if required.  `timeout` is still supported
to accept multiple associations and associate al received DICOM instances with a Clara job.

