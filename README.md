<strong>An Emby Live TV Plugin for MediaPortal 1 - TVServer</strong>

In addition to Emby Server, this plugin requires a fully configured MediaPortal TVServer installation.
<ul>
	<li><a href="http://wiki.team-mediaportal.com/1_MEDIAPORTAL_1/1_Getting_Started/12_Installing_MediaPortal">MediaPortal installation guide</a></li>
	<li><a href="http://wiki.team-mediaportal.com/1_MEDIAPORTAL_1/13_Setup_Guides/2_TV_Setup">MediaPortal TVServer setup</a></li>
</ul>

<p>This has a prerequisite of installing MPExtended on the box that hosts MediaPortal. MPExtended v0.5.4 and v.0.6.0-beta are supported - However v0.6.0.4-beta for Emby is recommended, so download and install it from here:</p>

<ul>
	<li>v0.6.0.4 -&nbsp;<a href="https://github.com/puenktchen/MediaPortalTVPlugin/blob/master/sources/MPExtended-Service-0.6.0.4-Emby.zip">MPExtended-Service-0.6.0.4-Emby.zip</a></li>
</ul>

<p>MPExtended allows for testing of itself, please check that it is working correctly by opening the configuration (from the system tray icon) going to Troubleshooting and clicking the links displayed. You should see a browser with something like &quot;{&quot;ApiVersion&quot;:4,&quot;HasConnectionToTVServer&quot;:true,&quot;ServiceVersion&quot;:&quot;0.5.4&quot;}&quot; displayed (depending on the version installed). We are only interested (currently) in the TV and Streaming services.<br />
&nbsp;<br />
<strong>Installation</strong></p>

<ul>
	<li>Find the MediaPortal Live TV Plugin in the plugin catalog and install it</li>
	<li>After Emby restart go to Emby's settings page - Live TV. Add the MediaPortal plugin as a tuner source</li>
	<li>Configure the plugin, adding any authentication information and test the connection by clicking "Save".</li>
	<li>Once you've entered the correct connection settings, you will be able to select a Channel Group and a Streaming Profile. For the Streaming Profile "Direct" is recommended as this deffers transcoding to Emby</li>
	<li>Switch to the Emby client UI and you shoud be good to go</li>
</ul>

<p><strong>Known Limitations</strong></p>
<ul>
	<li>The plugin acts as pure tuner source for Emby</li>
	<li>No schedules can be created for MediaPortal TV Server</li>
	<li>No recordings will be imported from MediaPortal TV Server</li>
</ul>

<br />
<br />
<p>All credits go to the developers of <a href="https://github.com/MPExtended/MPExtended">MPxtended</a>, mainly <a href="https://github.com/oxan">oxan</a> and <a href="https://github.com/DieBagger">DieBagger</a>!</p>
