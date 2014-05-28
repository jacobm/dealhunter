/** @jsx React.DOM */ 

var React = require('react');
var Configuration = require('../constants/Configuration');
var AppActions = require('../actions/AppActions');
var ReactPropTypes = React.PropTypes;

var GoogleLoginButton = React.createClass({
    componentDidMount: function() {
	// global scope
	signInCallback = function(authResult) {
	    AppActions.loginUser(authResult);
	}.bind(this);

	(function () {
	    var po = document.createElement('script');
	    po.type = 'text/javascript';
	    po.async = true;
	    po.src = 'https://plus.google.com/js/client:plusone.js?onload=start';
	    var s = document.getElementsByTagName('script')[0];
	    s.parentNode.insertBefore(po, s);
	})();
    },
    render: function(){

	var clientId = Configuration.GOOGLE_CLIENT_ID;
	return (
	    <div id="signinButton">
		<span className="g-signin"
	           data-scope="https://www.googleapis.com/auth/plus.login"
	           data-clientid={clientId}
	           data-redirecturi="postmessage"
	           data-accesstype="online"
	           data-cookiepolicy="single_host_origin"
	           data-callback="signInCallback">
		</span>
   	    </div>
	);
    }
});

module.exports = GoogleLoginButton;
